using Google.Apis.Auth;
using NFC.Platform.Application.DTOs.Auth;
using NFC.Platform.Application.Extensions;

namespace NFC.Platform.Application.Services;

    public class AuthService(
        IUnitOfWork unitOfWork,
        ITokenService tokenService,
        IMessageService messageService,
        IEmailService emailService,
        IConfiguration configuration,
        IBackgroundJobClient backgroundJobClient,
        IMapper mapper) : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        private readonly ITokenService _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        private readonly IMessageService _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
        private readonly IEmailService _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        private readonly IBackgroundJobClient _backgroundJobClient = backgroundJobClient ?? throw new ArgumentNullException(nameof(backgroundJobClient));
        private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

        public async Task<ServiceResult<AuthDto>> LoginAsync(LoginRequest request)
        {
            var userRepo = _unitOfWork.Repository<User>();
            
            User? user = null;
            var query = userRepo.GetQueryable();
            if (query != null && query.Provider is Microsoft.EntityFrameworkCore.Query.IAsyncQueryProvider)
            {
                user = await query.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Email == request.Email && !u.IsDeleted);
            }
            else
            {
                var matchedUsers = await userRepo.FindAsync(u => u.Email == request.Email && !u.IsDeleted);
                user = matchedUsers.Count > 0 ? matchedUsers[0] : null;
            }

            if (user == null || !PasswordHasher.VerifyPassword(request.Password, user.PasswordHash))
            {
                return ServiceResult<AuthDto>.Unauthorized(_messageService.Get("InvalidCredentials"));
            }

            if (!user.IsEmailVerified)
            {
                return ServiceResult<AuthDto>.Fail(_messageService.Get("EmailNotVerified"), 400);
            }

            return await GenerateAuthResponseAsync(user, _messageService.Get("LoginSuccess"));
        }

        public async Task<ServiceResult<bool>> RegisterAsync(RegisterRequest request)
        {
            var tenantRepo = _unitOfWork.Repository<Tenant>();
            var userRepo = _unitOfWork.Repository<User>();
            var roleRepo = _unitOfWork.Repository<Role>();
            var userRoleRepo = _unitOfWork.Repository<UserRole>();
            var companyRepo = _unitOfWork.Repository<Company>();
            var profileRepo = _unitOfWork.Repository<UserProfile>();

            // Compute stable values before the transaction so they are available
            // for the background email job that is enqueued after commit.
            var effectiveUsername = !string.IsNullOrWhiteSpace(request.Username)
                ? request.Username
                : (request.CompanyName ?? request.Email.Split('@')[0]);

            var phoneNum = !string.IsNullOrWhiteSpace(request.Phone)
                ? request.Phone
                : (request.WhatsApp ?? string.Empty);

            var tenantName = request.AccountType == AccountType.CompanyAdmin
                ? (request.CompanyName ?? $"{effectiveUsername} Company")
                : $"{effectiveUsername}'s Tenant";

            // Check if user already exists
            bool userExists = false;
            var query = userRepo.GetQueryable();
            if (query != null && query.Provider is Microsoft.EntityFrameworkCore.Query.IAsyncQueryProvider)
            {
                userExists = await query.IgnoreQueryFilters().AnyAsync(u => u.Email == request.Email && !u.IsDeleted);
            }
            else
            {
                var existingUsers = await userRepo.FindAsync(u => u.Email == request.Email && !u.IsDeleted);
                userExists = existingUsers.Count > 0;
            }

            if (userExists)
            {
                return ServiceResult<bool>.Fail(_messageService.Get("UserAlreadyExists"), 400);
            }

            // OTP generated before the transaction so it can be enqueued after commit
            var otpCode = GenerateOtp();

        await _unitOfWork.BeginTransactionAsync();

        try
        {
            // 1. Determine target role (read-only lookup before any writes)
            var targetRole = request.AccountType == AccountType.CompanyAdmin
                ? AppRole.CompanyAdmin
                : AppRole.Customer;

            var roles = await roleRepo.FindAsync(r => r.Name == targetRole.ToString());
            var matchingRole = roles.Count > 0 ? roles[0] : null;

            // 2. Build entity graph in memory.
            //    BaseEntity.Id = Guid.NewGuid() so all IDs are available before SaveChanges.
            var tenant = new Tenant
            {
                Name = tenantName,
                IsActive = true
            };

            var user = new User
            {
                Username = effectiveUsername,
                Email = request.Email,
                PasswordHash = PasswordHasher.HashPassword(request.Password),
                AccountType = request.AccountType,
                PhoneNumber = phoneNum,
                IsEmailVerified = false,
                OtpHash = OtpHasher.HashOtp(otpCode),
                OtpExpiresAt = DateTime.UtcNow.AddMinutes(10),
                TenantId = tenant.Id
            };

            // 3. Company (CompanyAdmin only)
            Company? company = null;
            if (request.AccountType == AccountType.CompanyAdmin)
            {
                company = _mapper.Map<Company>(request) ?? new Company();
                company.Name = request.CompanyName ?? "Company";
                company.TenantId = tenant.Id;
                company.AdminUserId = user.Id;
                user.CompanyId = company.Id;
            }

            // 4. UserProfile
            var baseSlug  = SubdomainHelper.Slugify(effectiveUsername);
            var subdomain = await GenerateUniqueSubdomainAsync(baseSlug);

            var profile = new UserProfile
            {
                UserId = user.Id,
                TenantId = tenant.Id,
                FullName = effectiveUsername,
                ContactEmail = request.Email,
                WhatsApp = phoneNum,
                Phone = phoneNum,
                Address = request.Address,
                CompanyName = request.CompanyName ?? string.Empty,
                Subdomain = subdomain
            };

            // 5. Persist entire graph in one round-trip
            await tenantRepo.AddAsync(tenant);
            await userRepo.AddAsync(user);
            if (company != null) await companyRepo.AddAsync(company);
            await profileRepo.AddAsync(profile);

            if (matchingRole != null)
            {
                await userRoleRepo.AddAsync(new UserRole
                {
                    UserId = user.Id,
                    RoleId = matchingRole.Id
                });
            }

            await _unitOfWork.SaveChangesAsync();   // single DB round-trip
            await _unitOfWork.CommitTransactionAsync();

            // Enqueue email AFTER commit so the OTP is guaranteed persisted
            var currentCulture = CultureInfo.CurrentUICulture.Name;
            _backgroundJobClient.Enqueue<IEmailService>(x =>
                x.SendOtpVerificationEmailAsync(user.Email, otpCode, currentCulture));

            return ServiceResult<bool>.Success(true, _messageService.Get("OtpSent"));
        }
        catch (Exception)
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
        }

        public async Task<ServiceResult<bool>> RegisterWithGoogleAsync(GoogleRegisterRequest request)
        {
            GoogleJsonWebSignature.Payload payload;
            try
            {
                var clientId = _configuration["GoogleSettings:ClientId"];
                var validationSettings = !string.IsNullOrWhiteSpace(clientId)
                    ? new GoogleJsonWebSignature.ValidationSettings { Audience = new[] { clientId } }
                    : null;

            // C-05 fix: only skip audience validation when no ClientId is configured
            // (e.g., development without Google credentials).
            // Never silently retry on validation failure — that would allow tokens
            // issued for a different Google client to authenticate on this platform.
            if (!string.IsNullOrWhiteSpace(clientId))
            {
                payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, validationSettings);
            }
            else
            {
                // No client ID configured — skip audience check (development / CI only)
                payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken);
            }
            }
            catch (Exception)
            {
                return ServiceResult<bool>.Fail(_messageService.Get("GoogleTokenInvalid"), 400);
            }

            if (payload == null || string.IsNullOrWhiteSpace(payload.Email))
            {
                return ServiceResult<bool>.Fail(_messageService.Get("GoogleTokenInvalid"), 400);
            }

            var userRepo = _unitOfWork.Repository<User>();
            var tenantRepo = _unitOfWork.Repository<Tenant>();
            var roleRepo = _unitOfWork.Repository<Role>();
            var userRoleRepo = _unitOfWork.Repository<UserRole>();
            var companyRepo = _unitOfWork.Repository<Company>();
            var profileRepo = _unitOfWork.Repository<UserProfile>();

            // Check if user exists
            bool userExists = false;
            var query = userRepo.GetQueryable();
            if (query != null && query.Provider is Microsoft.EntityFrameworkCore.Query.IAsyncQueryProvider)
            {
                userExists = await query.IgnoreQueryFilters().AnyAsync(u => u.Email == payload.Email && !u.IsDeleted);
            }
            else
            {
                var existingUsers = await userRepo.FindAsync(u => u.Email == payload.Email && !u.IsDeleted);
                userExists = existingUsers.Count > 0;
            }

            if (userExists)
            {
                return ServiceResult<bool>.Fail(_messageService.Get("UserAlreadyExists"), 400);
            }

            await _unitOfWork.BeginTransactionAsync();
            try
            {
            // 2. Build entity graph in memory (IDs come from BaseEntity.Id = Guid.NewGuid())
            var otpCode = GenerateOtp();
            var username = !string.IsNullOrWhiteSpace(payload.Name) ? payload.Name : payload.Email.Split('@')[0];
            var tenantName = request.AccountType == AccountType.CompanyAdmin
                ? (request.CompanyName ?? "Company Tenant")
                : $"{username}'s Tenant";

            var tenant = new Tenant { Name = tenantName, IsActive = true };

            var user = new User
            {
                GoogleId = payload.Subject,
                Username = username,
                Email = payload.Email,
                PasswordHash = string.Empty,
                AccountType = request.AccountType,
                PhoneNumber = request.WhatsApp ?? string.Empty,
                IsEmailVerified = false,
                OtpHash = OtpHasher.HashOtp(otpCode),
                OtpExpiresAt = DateTime.UtcNow.AddMinutes(10),
                TenantId = tenant.Id
            };

            Company? company = null;
            if (request.AccountType == AccountType.CompanyAdmin)
            {
                company = new Company
                {
                    Name = request.CompanyName ?? "Company",
                    TenantId = tenant.Id,
                    AdminUserId = user.Id
                };
                user.CompanyId = company.Id;
            }

            var googleBaseSlug  = SubdomainHelper.Slugify(username);
            var googleSubdomain = await GenerateUniqueSubdomainAsync(googleBaseSlug);

            var profile = new UserProfile
            {
                UserId = user.Id,
                TenantId = tenant.Id,
                FullName = username,
                ContactEmail = payload.Email,
                ProfilePictureUrl = payload.Picture,
                WhatsApp = request.WhatsApp,
                Phone = request.WhatsApp,
                CompanyName = request.CompanyName ?? string.Empty,
                Subdomain = googleSubdomain
            };

            var targetRole = request.AccountType == AccountType.CompanyAdmin
                ? AppRole.CompanyAdmin
                : AppRole.Customer;

            var roles = await roleRepo.FindAsync(r => r.Name == targetRole.ToString());
            var matchingRole = roles.Count > 0 ? roles[0] : null;

            // 3. Persist entire graph in one round-trip
            await tenantRepo.AddAsync(tenant);
            await userRepo.AddAsync(user);
            if (company != null) await companyRepo.AddAsync(company);
            await profileRepo.AddAsync(profile);

            if (matchingRole != null)
            {
                await userRoleRepo.AddAsync(new UserRole { UserId = user.Id, RoleId = matchingRole.Id });
            }

            await _unitOfWork.SaveChangesAsync();   // single DB round-trip
            await _unitOfWork.CommitTransactionAsync();

            // Enqueue email AFTER commit
            var currentCulture = CultureInfo.CurrentUICulture.Name;
            _backgroundJobClient.Enqueue<IEmailService>(x =>
                x.SendOtpVerificationEmailAsync(user.Email, otpCode, currentCulture));

            return ServiceResult<bool>.Success(true, _messageService.Get("OtpSent"));
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
        }

        public async Task<ServiceResult<AuthDto>> VerifyOtpAsync(VerifyOtpRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.OtpCode))
                return ServiceResult<AuthDto>.Fail(_messageService.Get("OtpInvalid"), 400);

            var userRepo = _unitOfWork.Repository<User>();
            User? user = null;
            var query = userRepo.GetQueryable();

            if (query != null && query.Provider is Microsoft.EntityFrameworkCore.Query.IAsyncQueryProvider)
            {
                user = await query.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Email == request.Email && !u.IsDeleted);
            }
            else
            {
                var matched = await userRepo.FindAsync(u => u.Email == request.Email && !u.IsDeleted);
                user = matched.Count > 0 ? matched[0] : null;
            }

            if (user == null)
                return ServiceResult<AuthDto>.Fail(_messageService.Get("OtpInvalid"), 400);

            if (string.IsNullOrWhiteSpace(user.OtpHash) || !OtpHasher.VerifyOtp(request.OtpCode, user.OtpHash))
                return ServiceResult<AuthDto>.Fail(_messageService.Get("OtpInvalid"), 400);

            if (user.OtpExpiresAt.HasValue && user.OtpExpiresAt.Value < DateTime.UtcNow)
                return ServiceResult<AuthDto>.Fail(_messageService.Get("OtpExpired"), 400);

            user.IsEmailVerified = true;
            user.OtpHash = null;
            user.OtpExpiresAt = null;

            await _unitOfWork.SaveChangesAsync();
            return await GenerateAuthResponseAsync(user, _messageService.Get("OtpVerifiedSuccess"));
        }
        public async Task<ServiceResult<bool>> ResendOtpAsync(ResendOtpRequest request)
        {
            var userRepo = _unitOfWork.Repository<User>();
            User? user = null;
            var query = userRepo.GetQueryable();
            if (query != null && query.Provider is Microsoft.EntityFrameworkCore.Query.IAsyncQueryProvider)
            {
                user = await query.IgnoreQueryFilters().Include(u => u.UserProfile).FirstOrDefaultAsync(u => u.Email == request.Email && !u.IsDeleted);
            }
            else
            {
                var matched = await userRepo.FindAsync(u => u.Email == request.Email && !u.IsDeleted);
                user = matched.Count > 0 ? matched[0] : null;
            }

            if (user == null)
                return ServiceResult<bool>.NotFound(_messageService.Get("RecordNotFound"));

            var otpCode = GenerateOtp();
            user.OtpHash = OtpHasher.HashOtp(otpCode);
            user.OtpExpiresAt = DateTime.UtcNow.AddMinutes(10);

            await _unitOfWork.SaveChangesAsync();

            var currentCulture = CultureInfo.CurrentUICulture.Name;
            _backgroundJobClient.Enqueue<IEmailService>(x =>
                x.SendOtpVerificationEmailAsync(user.Email, otpCode, currentCulture));

            return ServiceResult<bool>.Success(true, _messageService.Get("OtpSent"));
        }


        public async Task<ServiceResult<AuthDto>> RefreshTokenAsync(RefreshTokenRequest request)
        {
            var tokenRepo = _unitOfWork.Repository<RefreshToken>();
            var userRepo = _unitOfWork.Repository<User>();

            var matchedTokens = await tokenRepo.FindAsync(rt => rt.Token == request.RefreshToken && !rt.IsRevoked);
            var token = matchedTokens.Count > 0 ? matchedTokens[0] : null;

            if (token == null || token.IsExpired)
            {
                return ServiceResult<AuthDto>.Unauthorized(_messageService.Get("InvalidRefreshToken"));
            }

            User? user = null;
            var query = userRepo.GetQueryable();
            if (query != null && query.Provider is Microsoft.EntityFrameworkCore.Query.IAsyncQueryProvider)
            {
                user = await query.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == token.UserId && !u.IsDeleted);
            }
            else
            {
                user = await userRepo.GetByIdAsync(token.UserId);
            }

            if (user == null)
            {
                return ServiceResult<AuthDto>.Unauthorized(_messageService.Get("InvalidRefreshToken"));
            }

            // Revoke the old token
            token.IsRevoked = true;

            // Generate and save new access + refresh tokens
            return await GenerateAuthResponseAsync(user, _messageService.Get("TokenRefreshed"));
        }

        public async Task<ServiceResult> RevokeTokenAsync(RefreshTokenRequest request)
        {
            var tokenRepo = _unitOfWork.Repository<RefreshToken>();
            var matchedTokens = await tokenRepo.FindAsync(rt => rt.Token == request.RefreshToken && !rt.IsRevoked);
            var token = matchedTokens.Count > 0 ? matchedTokens[0] : null;

            if (token != null)
            {
                token.IsRevoked = true;
                await _unitOfWork.SaveChangesAsync();
            }

            return ServiceResult.Success();
        }

        public async Task<ServiceResult> ForgotPasswordAsync(ForgotPasswordRequest request)
        {
            var userRepo = _unitOfWork.Repository<User>();
            
            User? user = null;
            var query = userRepo.GetQueryable();
            if (query != null && query.Provider is Microsoft.EntityFrameworkCore.Query.IAsyncQueryProvider)
            {
                user = await query.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Email == request.Email && !u.IsDeleted);
            }
            else
            {
                var matchedUsers = await userRepo.FindAsync(u => u.Email == request.Email && !u.IsDeleted);
                user = matchedUsers.Count > 0 ? matchedUsers[0] : null;
            }

            if (user != null)
            {
                var resetToken = Guid.NewGuid().ToString("N");
                user.PasswordResetToken = resetToken;
                user.PasswordResetTokenExpires = DateTime.UtcNow.AddHours(1);

                await _unitOfWork.SaveChangesAsync();

                // Log the reset token in console/logs so it can be easily copied and tested by the developer (development environment only)
                if (string.Equals(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), "Development", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"[TESTING ONLY] Reset password token for {user.Email} is: {resetToken}");
                }

                // Send reset email asynchronously using Hangfire
                var clientUrl = _configuration["ClientSettings:ResetPasswordUrl"];
                var resetLink = $"{clientUrl}?token={resetToken}&email={Uri.EscapeDataString(user.Email)}";
                var culture = CultureInfo.CurrentUICulture.Name;
                _backgroundJobClient.Enqueue<IEmailService>(x => x.SendPasswordResetEmailAsync(user.Email, resetLink, culture));
            }

            // Always return success to prevent email enumeration/discovery attacks
            return ServiceResult.Success(_messageService.Get("PasswordResetRequested"));
        }

        public async Task<ServiceResult> ResetPasswordAsync(ResetPasswordRequest request)
        {
            var userRepo = _unitOfWork.Repository<User>();
            
            User? user = null;
            var query = userRepo.GetQueryable();
            if (query != null && query.Provider is Microsoft.EntityFrameworkCore.Query.IAsyncQueryProvider)
            {
                user = await query.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.PasswordResetToken == request.Token && !u.IsDeleted);
            }
            else
            {
                var matchedUsers = await userRepo.FindAsync(u => u.PasswordResetToken == request.Token && !u.IsDeleted);
                user = matchedUsers.Count > 0 ? matchedUsers[0] : null;
            }

            if (user == null)
            {
                return ServiceResult.Fail(_messageService.Get("InvalidResetToken"), 400);
            }

            if (user.PasswordResetTokenExpires < DateTime.UtcNow)
            {
                return ServiceResult.Fail(_messageService.Get("ResetTokenExpired"), 400);
            }

            user.PasswordHash = PasswordHasher.HashPassword(request.NewPassword);
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpires = null;

            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.Success(_messageService.Get("PasswordResetSuccess"));
        }

        public async Task<ServiceResult<UserDto>> CreateUserByAdminAsync(AdminCreateUserRequest request)
        {
            var userRepo = _unitOfWork.Repository<User>();
            var roleRepo = _unitOfWork.Repository<Role>();
            var userRoleRepo = _unitOfWork.Repository<UserRole>();

            bool userExists = false;
            var query = userRepo.GetQueryable();
            if (query != null && query.Provider is Microsoft.EntityFrameworkCore.Query.IAsyncQueryProvider)
            {
                userExists = await query.IgnoreQueryFilters().AnyAsync(u => u.Email == request.Email && !u.IsDeleted);
            }
            else
            {
                var existingUsers = await userRepo.FindAsync(u => u.Email == request.Email && !u.IsDeleted);
                userExists = existingUsers.Count > 0;
            }

            if (userExists)
            {
                return ServiceResult<UserDto>.Fail(_messageService.Get("UserAlreadyExists"), 400);
            }

            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = PasswordHasher.HashPassword(request.Password)
            };

            await userRepo.AddAsync(user);
            await _unitOfWork.SaveChangesAsync();

            var targetRoleName = request.Role.ToString();
            var roles = await roleRepo.FindAsync(r => r.Name == targetRoleName);
            var roleEntity = roles.Count > 0 ? roles[0] : null;

            if (roleEntity == null)
            {
                return ServiceResult<UserDto>.Fail(_messageService.Get("InvalidRole"), 400);
            }

            await userRoleRepo.AddAsync(new UserRole
            {
                UserId = user.Id,
                RoleId = roleEntity.Id
            });
            await _unitOfWork.SaveChangesAsync();

            var userDto = _mapper.Map<UserDto>(user);
            userDto.Role = targetRoleName;

            var culture = CultureInfo.CurrentUICulture.Name;
            _backgroundJobClient.Enqueue<IEmailService>(x => x.SendNewUserCredentialsEmailAsync(user.Email, user.Username, request.Password, culture));

            return ServiceResult<UserDto>.Success(userDto, _messageService.Get("UserCreated"));
        }

        //  Private Helper 

        private async Task<ServiceResult<AuthDto>> GenerateAuthResponseAsync(User user, string? message = null)
        {
            var userRoleRepo = _unitOfWork.Repository<UserRole>();
            var roleRepo = _unitOfWork.Repository<Role>();
            var tokenRepo = _unitOfWork.Repository<RefreshToken>();

            // Get user roles
            var userRoles = await userRoleRepo.FindAsync(ur => ur.UserId == user.Id);
            var roleIds = userRoles.Select(ur => ur.RoleId).ToList();
            var roles = await roleRepo.FindAsync(r => roleIds.Contains(r.Id));
            var roleNames = roles.Select(r => r.Name).ToList();

            // Generate tokens
            var accessToken = _tokenService.GenerateToken(user.Id, user.Email, roleNames, user.TenantId, user.CompanyId, user.AccountType.ToString());
            var refreshTokenString = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));


            var newRefreshToken = new RefreshToken
            {
                Token = refreshTokenString,
                UserId = user.Id,
                TenantId = user.TenantId,
                ExpiresOn = DateTime.UtcNow.AddDays(7),
                IsRevoked = false
            };

            await tokenRepo.AddAsync(newRefreshToken);
            await _unitOfWork.SaveChangesAsync();

            var authDto = new AuthDto
            {
                Token = accessToken,
                RefreshToken = refreshTokenString,
                Username = user.Username,
                Email = user.Email,
                IsEmailVerified = user.IsEmailVerified,
                Roles = roleNames
            };

            return ServiceResult<AuthDto>.Success(authDto, message);
        }

        private static string GenerateOtp()
        {
            // Cryptographically secure 6-digit OTP spanning 000000–999999
            var bytes = RandomNumberGenerator.GetBytes(4);
            var value = BitConverter.ToUInt32(bytes, 0) % 1_000_000;
            return value.ToString("D6");
        }

        /// <summary>
        /// Generates a unique subdomain slug by starting with <paramref name="baseSlug"/>
        /// and appending a random 4-digit suffix if the candidate is already taken.
        /// </summary>
        private async Task<string> GenerateUniqueSubdomainAsync(string baseSlug)
        {
            var candidate = baseSlug;
            var profileRepo = _unitOfWork.Repository<UserProfile>();

            while (true)
            {
                var query = profileRepo.GetQueryable();
                bool taken;

                if (query != null && query.Provider is Microsoft.EntityFrameworkCore.Query.IAsyncQueryProvider)
                    taken = await query.IgnoreQueryFilters().AnyAsync(p => p.Subdomain == candidate);
                else
                    taken = (await profileRepo.FindAsync(p => p.Subdomain == candidate)).Count > 0;

                if (!taken) return candidate;

                candidate = $"{baseSlug}-{Random.Shared.Next(1000, 9999)}";
            }
        }
    }
