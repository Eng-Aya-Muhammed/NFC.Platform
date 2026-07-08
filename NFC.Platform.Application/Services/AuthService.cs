using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Application.Interfaces.Repositories;
using NFC.Platform.Application.Interfaces.Services;
using NFC.Platform.BuildingBlocks.Common.Helpers;
using NFC.Platform.BuildingBlocks.Localization;
using NFC.Platform.BuildingBlocks.Results;
using NFC.Platform.Domain.Entities;
using NFC.Platform.Domain.Enums;

namespace NFC.Platform.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITokenService _tokenService;
        private readonly IMessageService _messageService;

        public AuthService(
            IUnitOfWork unitOfWork,
            ITokenService tokenService,
            IMessageService messageService)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
            _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
        }

        public async Task<ServiceResult<AuthDto>> LoginAsync(LoginRequest request)
        {
            var userRepo = _unitOfWork.Repository<User>();
            var matchedUsers = await userRepo.FindAsync(u => u.Email == request.Email);
            var user = matchedUsers.Count > 0 ? matchedUsers[0] : null;

            if (user == null || !PasswordHasher.VerifyPassword(request.Password, user.PasswordHash))
            {
                return ServiceResult<AuthDto>.Unauthorized(_messageService.Get("InvalidCredentials"));
            }

            return await GenerateAuthResponseAsync(user, _messageService.Get("LoginSuccess"));
        }

        public async Task<ServiceResult<AuthDto>> RegisterAsync(RegisterRequest request)
        {
            var tenantRepo = _unitOfWork.Repository<Tenant>();
            var userRepo = _unitOfWork.Repository<User>();
            var roleRepo = _unitOfWork.Repository<Role>();
            var userRoleRepo = _unitOfWork.Repository<UserRole>();
            var companyRepo = _unitOfWork.Repository<Company>();

            // Check if user already exists
            var existingUsers = await userRepo.FindAsync(u => u.Email == request.Email);
            if (existingUsers.Count > 0)
            {
                return ServiceResult<AuthDto>.Fail(_messageService.Get("UserAlreadyExists"), 400);
            }

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                // 1. Create Tenant
                var tenantName = request.AccountType == AccountType.CompanyAdmin
                    ? (request.CompanyName ?? "Company Tenant")
                    : $"{request.Username}'s Tenant";

                var tenant = new Tenant
                {
                    Name = tenantName,
                    IsActive = true
                };

                await tenantRepo.AddAsync(tenant);
                await _unitOfWork.SaveChangesAsync();

                // 2. Create User
                var user = new User
                {
                    Username = request.Username,
                    Email = request.Email,
                    PasswordHash = PasswordHasher.HashPassword(request.Password),
                    AccountType = request.AccountType,
                    TenantId = tenant.Id
                };

                await userRepo.AddAsync(user);
                await _unitOfWork.SaveChangesAsync();

                // 3. Create Company if CompanyAdmin
                if (request.AccountType == AccountType.CompanyAdmin)
                {
                    var company = new Company
                    {
                        Name = request.CompanyName ?? "Company",
                        TenantId = tenant.Id,
                        AdminUserId = user.Id
                    };
                    await companyRepo.AddAsync(company);
                    await _unitOfWork.SaveChangesAsync();

                    user.CompanyId = company.Id;
                    userRepo.Update(user);
                    await _unitOfWork.SaveChangesAsync();
                }

                // 4. Assign Role
                var targetRole = request.AccountType == AccountType.CompanyAdmin
                    ? AppRole.CompanyAdmin
                    : AppRole.Customer;

                var roles = await roleRepo.FindAsync(r => r.Name == targetRole.ToString());
                var matchingRole = roles.Count > 0 ? roles[0] : null;

                if (matchingRole != null)
                {
                    await userRoleRepo.AddAsync(new UserRole
                    {
                        UserId = user.Id,
                        RoleId = matchingRole.Id
                    });
                    await _unitOfWork.SaveChangesAsync();
                }

                await _unitOfWork.CommitTransactionAsync();

                return await GenerateAuthResponseAsync(user, _messageService.Get("RegisterSuccess"));
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
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

            var user = await userRepo.GetByIdAsync(token.UserId);
            if (user == null)
            {
                return ServiceResult<AuthDto>.Unauthorized(_messageService.Get("InvalidRefreshToken"));
            }

            // Revoke the old token
            token.IsRevoked = true;
            tokenRepo.Update(token);

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
                tokenRepo.Update(token);
                await _unitOfWork.SaveChangesAsync();
            }

            return ServiceResult.Success();
        }

        public async Task<ServiceResult> ForgotPasswordAsync(ForgotPasswordRequest request)
        {
            var userRepo = _unitOfWork.Repository<User>();
            var matchedUsers = await userRepo.FindAsync(u => u.Email == request.Email);
            var user = matchedUsers.Count > 0 ? matchedUsers[0] : null;

            if (user != null)
            {
                var resetToken = Guid.NewGuid().ToString("N");
                user.PasswordResetToken = resetToken;
                user.PasswordResetTokenExpires = DateTime.UtcNow.AddHours(1);

                userRepo.Update(user);
                await _unitOfWork.SaveChangesAsync();

                // Log the reset token in console/logs so it can be easily copied and tested by the developer
                Console.WriteLine($"[TESTING ONLY] Reset password token for {user.Email} is: {resetToken}");
            }

            // Always return success to prevent email enumeration/discovery attacks
            return ServiceResult.Success(_messageService.Get("PasswordResetRequested"));
        }

        public async Task<ServiceResult> ResetPasswordAsync(ResetPasswordRequest request)
        {
            var userRepo = _unitOfWork.Repository<User>();
            var matchedUsers = await userRepo.FindAsync(u => u.PasswordResetToken == request.Token);
            var user = matchedUsers.Count > 0 ? matchedUsers[0] : null;

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

            userRepo.Update(user);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.Success(_messageService.Get("PasswordResetSuccess"));
        }

        public async Task<ServiceResult<UserDto>> CreateUserByAdminAsync(AdminCreateUserRequest request)
        {
            var userRepo = _unitOfWork.Repository<User>();
            var roleRepo = _unitOfWork.Repository<Role>();
            var userRoleRepo = _unitOfWork.Repository<UserRole>();

            var existingUsers = await userRepo.FindAsync(u => u.Email == request.Email);
            if (existingUsers.Count > 0)
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

            var userDto = new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Role = targetRoleName
            };

            return ServiceResult<UserDto>.Success(userDto, _messageService.Get("UserCreated"));
        }

        // ─── Private Helper ───────────────────────────────────────────────────

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
                Roles = roleNames
            };

            return ServiceResult<AuthDto>.Success(authDto, message);
        }
    }
}
