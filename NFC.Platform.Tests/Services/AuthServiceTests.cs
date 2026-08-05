namespace NFC.Platform.Tests.Services
{
    public class AuthServiceTests
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITokenService _tokenService;
        private readonly IMessageService _messageService;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly IMapper _mapper;
        private readonly IGenericRepository<User> _userRepo;
        private readonly IGenericRepository<Role> _roleRepo;
        private readonly IGenericRepository<UserRole> _userRoleRepo;
        private readonly IGenericRepository<RefreshToken> _tokenRepo;
        private readonly AuthService _sut;

        public AuthServiceTests()
        {
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _tokenService = Substitute.For<ITokenService>();
            _messageService = Substitute.For<IMessageService>();
            _emailService = Substitute.For<IEmailService>();
            _configuration = Substitute.For<IConfiguration>();
            _backgroundJobClient = Substitute.For<IBackgroundJobClient>();
            _mapper = Substitute.For<IMapper>();

            _userRepo = Substitute.For<IGenericRepository<User>>();
            _roleRepo = Substitute.For<IGenericRepository<Role>>();
            _userRoleRepo = Substitute.For<IGenericRepository<UserRole>>();
            _tokenRepo = Substitute.For<IGenericRepository<RefreshToken>>();

            _unitOfWork.Repository<User>().Returns(_userRepo);
            _unitOfWork.Repository<Role>().Returns(_roleRepo);
            _unitOfWork.Repository<UserRole>().Returns(_userRoleRepo);
            _unitOfWork.Repository<RefreshToken>().Returns(_tokenRepo);

            _sut = new AuthService(_unitOfWork, _tokenService, _messageService, _emailService, _configuration, _backgroundJobClient, _mapper);
        }

        [Fact]
        public async Task LoginAsync_ReturnsUnauthorized_WhenUserDoesNotExist()
        {
            var request = new LoginRequest { Email = "notfound@test.com", Password = "Password123!" };
            _userRepo.FindAsync(Arg.Any<Expression<Func<User, bool>>>())
                .Returns(new List<User>());
            _messageService.Get("InvalidCredentials").Returns("Invalid email or password.");

            var result = await _sut.LoginAsync(request);

            Assert.False(result.IsSuccess);
            Assert.Equal(401, result.StatusCode);
            Assert.Equal("Invalid email or password.", result.Message);
        }

        [Fact]
        public async Task LoginAsync_ReturnsSuccess_WhenCredentialsAreValid()
        {
            var password = "Password123!";
            var hashedPassword = PasswordHasher.HashPassword(password);
            var user = new User { Email = "user@test.com", PasswordHash = hashedPassword, Username = "testuser", IsEmailVerified = true };

            var request = new LoginRequest { Email = "user@test.com", Password = password };

            _userRepo.FindAsync(Arg.Any<Expression<Func<User, bool>>>())
                .Returns(new List<User> { user });

            _userRoleRepo.FindAsync(Arg.Any<Expression<Func<UserRole, bool>>>())
                .Returns(new List<UserRole>());
            _roleRepo.FindAsync(Arg.Any<Expression<Func<Role, bool>>>())
                .Returns(new List<Role>());

            _tokenService.GenerateToken(user.Id, user.Email, Arg.Any<IEnumerable<string>>(), Arg.Any<Guid>(), Arg.Any<Guid?>(), Arg.Any<string>())
                .Returns("mock-access-token");
            _messageService.Get("LoginSuccess").Returns("Logged in successfully.");

            var result = await _sut.LoginAsync(request);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal("mock-access-token", result.Data.Token);
            Assert.NotEmpty(result.Data.RefreshToken);
            Assert.Equal("Logged in successfully.", result.Message);
            await _tokenRepo.Received(1).AddAsync(Arg.Any<RefreshToken>());
            await _unitOfWork.Received().SaveChangesAsync();
        }

        [Fact]
        public async Task RegisterAsync_ReturnsBadRequest_WhenUserAlreadyExists()
        {
            var request = new RegisterRequest { Email = "exists@test.com", Username = "user", Password = "123" };
            var existingUser = new User { Email = "exists@test.com" };

            _userRepo.FindAsync(Arg.Any<Expression<Func<User, bool>>>())
                .Returns(new List<User> { existingUser });
            _messageService.Get("UserAlreadyExists").Returns("User already exists.");

            var result = await _sut.RegisterAsync(request);

            Assert.False(result.IsSuccess);
            Assert.Equal(400, result.StatusCode);
            Assert.Equal("User already exists.", result.Message);
        }

        [Fact]
        public async Task RefreshTokenAsync_ReturnsUnauthorized_WhenTokenIsExpired()
        {
            var expiredToken = new RefreshToken
            {
                Token = "expired-token",
                ExpiresOn = DateTime.UtcNow.AddMinutes(-5),
                IsRevoked = false
            };
            var request = new RefreshTokenRequest { RefreshToken = "expired-token" };

            _tokenRepo.FindAsync(Arg.Any<Expression<Func<RefreshToken, bool>>>())
                .Returns(new List<RefreshToken> { expiredToken });
            _messageService.Get("InvalidRefreshToken").Returns("Invalid or expired refresh token.");

            var result = await _sut.RefreshTokenAsync(request);

            Assert.False(result.IsSuccess);
            Assert.Equal(401, result.StatusCode);
        }

        [Fact]
        public async Task ForgotPasswordAsync_UpdatesResetToken_WhenUserExists()
        {
            var email = "user@test.com";
            var user = new User { Email = email };
            var request = new ForgotPasswordRequest { Email = email };

            _configuration["ClientSettings:ResetPasswordUrl"].Returns("http://localhost:3000/reset-password");

            _userRepo.FindAsync(Arg.Any<Expression<Func<User, bool>>>())
                .Returns(new List<User> { user });

            var result = await _sut.ForgotPasswordAsync(request);

            Assert.True(result.IsSuccess);
            Assert.NotNull(user.PasswordResetToken);
            Assert.NotNull(user.PasswordResetTokenExpires);
            await _unitOfWork.Received().SaveChangesAsync();
            _backgroundJobClient.Received(1).Create(
                Arg.Is<Job>(job => job.Method.Name == nameof(IEmailService.SendPasswordResetEmailAsync) &&
                                   (string)job.Args[0] == email &&
                                   ((string)job.Args[1]).Contains(user.PasswordResetToken!) &&
                                   (string)job.Args[2] == CultureInfo.CurrentUICulture.Name),
                Arg.Any<IState>());
        }

        [Fact]
        public async Task ResetPasswordAsync_ChangesPassword_WhenTokenIsValid()
        {
            var token = "valid-reset-token";
            var user = new User
            {
                Email = "user@test.com",
                PasswordResetToken = token,
                PasswordResetTokenExpires = DateTime.UtcNow.AddHours(1)
            };
            var request = new ResetPasswordRequest
            {
                Token = token,
                NewPassword = "NewSecurePassword123!"
            };

            _userRepo.FindAsync(Arg.Any<Expression<Func<User, bool>>>())
                .Returns(new List<User> { user });

            var result = await _sut.ResetPasswordAsync(request);

            Assert.True(result.IsSuccess);
            Assert.Null(user.PasswordResetToken);
            Assert.Null(user.PasswordResetTokenExpires);
            Assert.True(PasswordHasher.VerifyPassword("NewSecurePassword123!", user.PasswordHash));
            await _unitOfWork.Received().SaveChangesAsync();
        }

        [Fact]
        public async Task RegisterAsync_ReturnsSuccess_WhenRequestIsValid()
        {
            var request = new RegisterRequest
            {
                Email = "newuser@test.com",
                Username = "newuser",
                Password = "Password123!"
            };

            _userRepo.FindAsync(Arg.Any<Expression<Func<User, bool>>>())
                .Returns(new List<User>());

            _roleRepo.FindAsync(Arg.Any<Expression<Func<Role, bool>>>())
                .Returns(new List<Role> { new() { Id = Guid.NewGuid(), Name = AppRole.Customer.ToString() } });

            _userRoleRepo.FindAsync(Arg.Any<Expression<Func<UserRole, bool>>>())
                .Returns(new List<UserRole>());

            _tokenService.GenerateToken(Arg.Any<Guid>(), request.Email, Arg.Any<IEnumerable<string>>(), Arg.Any<Guid>(), Arg.Any<Guid?>(), Arg.Any<string>())
                .Returns("mock-access-token");

            var result = await _sut.RegisterAsync(request);

            Assert.True(result.IsSuccess);
            Assert.True(result.Data);
            await _userRepo.Received(1).AddAsync(Arg.Any<User>());
            await _userRoleRepo.Received(1).AddAsync(Arg.Any<UserRole>());
        }

        [Fact]
        public async Task RegisterAsync_CreatesCompanyAndAssignsRole_WhenAccountTypeIsCompanyAdmin()
        {
            var request = new RegisterRequest
            {
                Email = "companyadmin@test.com",
                Username = "companyadmin",
                Password = "Password123!",
                AccountType = AccountType.CompanyAdmin,
                CompanyName = "Test Company"
            };

            var companyRepo = Substitute.For<IGenericRepository<Company>>();
            _unitOfWork.Repository<Company>().Returns(companyRepo);

            _userRepo.FindAsync(Arg.Any<Expression<Func<User, bool>>>())
                .Returns(new List<User>());

            _roleRepo.FindAsync(Arg.Any<Expression<Func<Role, bool>>>())
                .Returns(new List<Role> { new() { Id = Guid.NewGuid(), Name = AppRole.CompanyAdmin.ToString() } });

            _userRoleRepo.FindAsync(Arg.Any<Expression<Func<UserRole, bool>>>())
                .Returns(new List<UserRole>());

            _tokenService.GenerateToken(Arg.Any<Guid>(), request.Email, Arg.Any<IEnumerable<string>>(), Arg.Any<Guid>(), Arg.Any<Guid?>(), Arg.Any<string>())
                .Returns("mock-access-token");

            var result = await _sut.RegisterAsync(request);

            Assert.True(result.IsSuccess);
            Assert.True(result.Data);
            await _userRepo.Received(1).AddAsync(Arg.Any<User>());
            await companyRepo.Received(1).AddAsync(Arg.Any<Company>());
            await _userRoleRepo.Received(1).AddAsync(Arg.Any<UserRole>());
        }

        [Fact]
        public async Task RefreshTokenAsync_ReturnsSuccess_WhenTokenIsValid()
        {
            var userId = Guid.NewGuid();
            var validRefreshToken = new RefreshToken
            {
                Token = "valid-refresh-token",
                UserId = userId,
                ExpiresOn = DateTime.UtcNow.AddDays(1),
                IsRevoked = false
            };
            var request = new RefreshTokenRequest { RefreshToken = "valid-refresh-token" };
            var user = new User { Id = userId, Email = "user@test.com", Username = "testuser" };

            _tokenRepo.FindAsync(Arg.Any<Expression<Func<RefreshToken, bool>>>())
                .Returns(new List<RefreshToken> { validRefreshToken });

            _userRepo.GetByIdAsync(userId).Returns(user);

            _userRoleRepo.FindAsync(Arg.Any<Expression<Func<UserRole, bool>>>())
                .Returns(new List<UserRole>());
            _roleRepo.FindAsync(Arg.Any<Expression<Func<Role, bool>>>())
                .Returns(new List<Role>());

            _tokenService.GenerateToken(userId, user.Email, Arg.Any<IEnumerable<string>>(), Arg.Any<Guid>(), Arg.Any<Guid?>(), Arg.Any<string>())
                .Returns("new-access-token");
            _messageService.Get("TokenRefreshed").Returns("Token refreshed successfully.");

            var result = await _sut.RefreshTokenAsync(request);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal("new-access-token", result.Data.Token);
            Assert.Equal("Token refreshed successfully.", result.Message);
            Assert.True(validRefreshToken.IsRevoked);
            await _tokenRepo.Received(1).AddAsync(Arg.Any<RefreshToken>());
            await _unitOfWork.Received().SaveChangesAsync();
        }

        [Fact]
        public async Task RevokeTokenAsync_RevokesToken_WhenTokenExists()
        {
            var token = new RefreshToken { Token = "token-to-revoke", IsRevoked = false };
            var request = new RefreshTokenRequest { RefreshToken = "token-to-revoke" };

            _tokenRepo.FindAsync(Arg.Any<Expression<Func<RefreshToken, bool>>>())
                .Returns(new List<RefreshToken> { token });

            var result = await _sut.RevokeTokenAsync(request);

            Assert.True(result.IsSuccess);
            Assert.True(token.IsRevoked);
            await _unitOfWork.Received(1).SaveChangesAsync();
        }

        [Fact]
        public async Task ResetPasswordAsync_ReturnsBadRequest_WhenTokenIsExpired()
        {
            var token = "expired-reset-token";
            var user = new User
            {
                Email = "user@test.com",
                PasswordResetToken = token,
                PasswordResetTokenExpires = DateTime.UtcNow.AddMinutes(-5)
            };
            var request = new ResetPasswordRequest
            {
                Token = token,
                NewPassword = "NewSecurePassword123!"
            };

            _userRepo.FindAsync(Arg.Any<Expression<Func<User, bool>>>())
                .Returns(new List<User> { user });

            _messageService.Get("ResetTokenExpired").Returns("Reset token has expired.");

            var result = await _sut.ResetPasswordAsync(request);

            Assert.False(result.IsSuccess);
            Assert.Equal(400, result.StatusCode);
            Assert.Equal("Reset token has expired.", result.Message);
        }

        [Fact]
        public async Task ResetPasswordAsync_ReturnsBadRequest_WhenTokenDoesNotExist()
        {
            var request = new ResetPasswordRequest
            {
                Token = "non-existent-token",
                NewPassword = "NewSecurePassword123!"
            };

            _userRepo.FindAsync(Arg.Any<Expression<Func<User, bool>>>())
                .Returns(new List<User>());
            _messageService.Get("InvalidResetToken").Returns("Invalid reset token.");

            var result = await _sut.ResetPasswordAsync(request);

            Assert.False(result.IsSuccess);
            Assert.Equal(400, result.StatusCode);
            Assert.Equal("Invalid reset token.", result.Message);
        }

        [Fact]
        public async Task CreateUserByAdminAsync_ReturnsSuccess_WhenAdminCreatesValidUser()
        {
            var request = new AdminCreateUserRequest
            {
                Email = "newadmin@test.com",
                Username = "newadmin",
                Password = "Password123!",
                Role = AppRole.Admin
            };

            _userRepo.FindAsync(Arg.Any<Expression<Func<User, bool>>>())
                .Returns(new List<User>());

            _roleRepo.FindAsync(Arg.Any<Expression<Func<Role, bool>>>())
                .Returns(new List<Role> { new() { Id = Guid.NewGuid(), Name = AppRole.Admin.ToString() } });

            _userRoleRepo.FindAsync(Arg.Any<Expression<Func<UserRole, bool>>>())
                .Returns(new List<UserRole>());

            _messageService.Get("UserCreated").Returns("User created successfully.");

            var expectedUserDto = new UserDto { Username = "newadmin", Email = "newadmin@test.com" };
            _mapper.Map<UserDto>(Arg.Any<User>()).Returns(expectedUserDto);

            var result = await _sut.CreateUserByAdminAsync(request);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal("newadmin", result.Data.Username);
            Assert.Equal(AppRole.Admin.ToString(), result.Data.Role);
            Assert.Equal("User created successfully.", result.Message);
            await _userRepo.Received(1).AddAsync(Arg.Any<User>());
            await _userRoleRepo.Received(1).AddAsync(Arg.Any<UserRole>());
            await _unitOfWork.Received(2).SaveChangesAsync();
            _backgroundJobClient.Received(1).Create(
                Arg.Is<Job>(job => job.Method.Name == nameof(IEmailService.SendNewUserCredentialsEmailAsync) &&
                                   (string)job.Args[0] == request.Email &&
                                   (string)job.Args[1] == request.Username &&
                                   (string)job.Args[2] == request.Password &&
                                   (string)job.Args[3] == CultureInfo.CurrentUICulture.Name),
                Arg.Any<IState>());
        }

        [Fact]
        public async Task CreateUserByAdminAsync_ReturnsBadRequest_WhenUserAlreadyExists()
        {
            var request = new AdminCreateUserRequest
            {
                Email = "exists@test.com",
                Username = "user",
                Password = "Password123!",
                Role = AppRole.Customer
            };
            var existingUser = new User { Email = "exists@test.com" };

            _userRepo.FindAsync(Arg.Any<Expression<Func<User, bool>>>())
                .Returns(new List<User> { existingUser });

            _messageService.Get("UserAlreadyExists").Returns("User already exists.");

            var result = await _sut.CreateUserByAdminAsync(request);

            Assert.False(result.IsSuccess);
            Assert.Equal(400, result.StatusCode);
            Assert.Equal("User already exists.", result.Message);
        }

        [Fact]
        public async Task ForgotPasswordAsync_SetsTokenAndExpires_WhenUserExists()
        {
            var user = new User { Email = "user@test.com" };
            _userRepo.GetQueryable().Returns(new List<User> { user }.AsQueryable().BuildMock());

            var result = await _sut.ForgotPasswordAsync(new ForgotPasswordRequest { Email = "user@test.com" });

            Assert.True(result.IsSuccess);
            Assert.NotNull(user.PasswordResetToken);
            Assert.NotNull(user.PasswordResetTokenExpires);
            await _unitOfWork.Received(1).SaveChangesAsync();
        }

        [Fact]
        public async Task ForgotPasswordAsync_AlwaysReturnsSuccess_EvenWhenUserDoesNotExist()
        {
            _userRepo.GetQueryable().Returns(new List<User>().AsQueryable().BuildMock());

            var result = await _sut.ForgotPasswordAsync(new ForgotPasswordRequest { Email = "nonexistent@test.com" });

            Assert.True(result.IsSuccess);
            await _unitOfWork.DidNotReceive().SaveChangesAsync();
        }

        [Fact]
        public async Task ResetPasswordAsync_Returns400_WhenTokenIsInvalid()
        {
            _userRepo.GetQueryable().Returns(new List<User>().AsQueryable().BuildMock());

            var result = await _sut.ResetPasswordAsync(new ResetPasswordRequest { Token = "invalid", NewPassword = "NewPassword123!" });

            Assert.False(result.IsSuccess);
            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public async Task ResetPasswordAsync_Returns400_WhenTokenIsExpired()
        {
            var expiredUser = new User
            {
                PasswordResetToken = "expired",
                PasswordResetTokenExpires = DateTime.UtcNow.AddMinutes(-10)
            };
            _userRepo.GetQueryable().Returns(new List<User> { expiredUser }.AsQueryable().BuildMock());

            var result = await _sut.ResetPasswordAsync(new ResetPasswordRequest { Token = "expired", NewPassword = "NewPassword123!" });

            Assert.False(result.IsSuccess);
            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public async Task ResetPasswordAsync_ResetsPasswordSuccessfully()
        {
            var user = new User
            {
                PasswordResetToken = "valid",
                PasswordResetTokenExpires = DateTime.UtcNow.AddMinutes(10)
            };
            _userRepo.GetQueryable().Returns(new List<User> { user }.AsQueryable().BuildMock());

            var result = await _sut.ResetPasswordAsync(new ResetPasswordRequest { Token = "valid", NewPassword = "NewPassword123!" });

            Assert.True(result.IsSuccess);
            Assert.Null(user.PasswordResetToken);
            Assert.Null(user.PasswordResetTokenExpires);
            Assert.True(NFC.Platform.BuildingBlocks.Common.Helpers.PasswordHasher.VerifyPassword("NewPassword123!", user.PasswordHash));
            await _unitOfWork.Received(1).SaveChangesAsync();
        }

        [Fact]
        public async Task CreateUserByAdminAsync_Returns400_WhenRoleDoesNotExist()
        {
            var request = new AdminCreateUserRequest
            {
                Username = "newuser",
                Email = "new@test.com",
                Password = "Password123!",
                Role = AppRole.Customer
            };
            _userRepo.GetQueryable().Returns(new List<User>().AsQueryable().BuildMock());
            _roleRepo.FindAsync(Arg.Any<Expression<Func<Role, bool>>>()).Returns(new List<Role>());

            var result = await _sut.CreateUserByAdminAsync(request);

            Assert.False(result.IsSuccess);
            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public async Task CreateUserByAdminAsync_CreatesUserAndRoleSuccessfully()
        {
            var request = new AdminCreateUserRequest
            {
                Username = "newuser",
                Email = "new@test.com",
                Password = "Password123!",
                Role = AppRole.Customer
            };
            _userRepo.GetQueryable().Returns(new List<User>().AsQueryable().BuildMock());

            var role = new Role { Id = Guid.NewGuid(), Name = "Customer" };
            _roleRepo.FindAsync(Arg.Any<Expression<Func<Role, bool>>>()).Returns(new List<Role> { role });

            var expectedUserDto = new UserDto { Username = "newuser", Email = "new@test.com" };
            _mapper.Map<UserDto>(Arg.Any<User>()).Returns(expectedUserDto);

            var result = await _sut.CreateUserByAdminAsync(request);

            Assert.True(result.IsSuccess);
            Assert.Equal("newuser", result.Data!.Username);
            Assert.Equal("new@test.com", result.Data.Email);
            await _userRepo.Received(1).AddAsync(Arg.Any<User>());
            await _userRoleRepo.Received(1).AddAsync(Arg.Is<UserRole>(ur => ur.RoleId == role.Id));
            await _unitOfWork.Received(2).SaveChangesAsync();
            _backgroundJobClient.Received(1).Create(
                Arg.Is<Job>(job => job.Method.Name == nameof(IEmailService.SendNewUserCredentialsEmailAsync) &&
                                   (string)job.Args[0] == request.Email &&
                                   (string)job.Args[1] == request.Username &&
                                   (string)job.Args[2] == request.Password &&
                                   (string)job.Args[3] == CultureInfo.CurrentUICulture.Name),
                Arg.Any<IState>());
        }

        [Fact]
        public async Task VerifyOtpAsync_ReturnsFail_WhenEmailOrOtpIsEmpty()
        {
            _messageService.Get("OtpInvalid").Returns("Invalid OTP.");

            var resultNoEmail = await _sut.VerifyOtpAsync(new VerifyOtpRequest { Email = "", OtpCode = "123456" });
            var resultNoOtp = await _sut.VerifyOtpAsync(new VerifyOtpRequest { Email = "user@test.com", OtpCode = "" });

            Assert.False(resultNoEmail.IsSuccess);
            Assert.Equal(400, resultNoEmail.StatusCode);
            Assert.False(resultNoOtp.IsSuccess);
            Assert.Equal(400, resultNoOtp.StatusCode);
        }

        [Fact]
        public async Task VerifyOtpAsync_ReturnsFail_WhenUserNotFound()
        {
            _userRepo.GetQueryable().Returns(new List<User>().AsQueryable().BuildMock());
            _messageService.Get("OtpInvalid").Returns("Invalid OTP.");

            var result = await _sut.VerifyOtpAsync(new VerifyOtpRequest { Email = "notfound@test.com", OtpCode = "123456" });

            Assert.False(result.IsSuccess);
            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public async Task VerifyOtpAsync_ReturnsFail_WhenOtpIsInvalid()
        {
            var user = new User { Email = "user@test.com", OtpHash = NFC.Platform.BuildingBlocks.Common.Helpers.OtpHasher.HashOtp("654321"), OtpExpiresAt = DateTime.UtcNow.AddMinutes(5) };
            _userRepo.GetQueryable().Returns(new List<User> { user }.AsQueryable().BuildMock());
            _messageService.Get("OtpInvalid").Returns("Invalid OTP.");

            var result = await _sut.VerifyOtpAsync(new VerifyOtpRequest { Email = "user@test.com", OtpCode = "123456" });

            Assert.False(result.IsSuccess);
            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public async Task VerifyOtpAsync_ReturnsFail_WhenUserIsAlreadyVerified_AndOtpIsInvalid()
        {
            var user = new User { Email = "user@test.com", IsEmailVerified = true, OtpHash = NFC.Platform.BuildingBlocks.Common.Helpers.OtpHasher.HashOtp("654321"), OtpExpiresAt = DateTime.UtcNow.AddMinutes(5) };
            _userRepo.GetQueryable().Returns(new List<User> { user }.AsQueryable().BuildMock());
            _messageService.Get("OtpInvalid").Returns("Invalid OTP.");

            var result = await _sut.VerifyOtpAsync(new VerifyOtpRequest { Email = "user@test.com", OtpCode = "000000" });

            Assert.False(result.IsSuccess);
            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public async Task VerifyOtpAsync_ReturnsFail_WhenOtpIsExpired()
        {
            var user = new User { Email = "user@test.com", OtpHash = NFC.Platform.BuildingBlocks.Common.Helpers.OtpHasher.HashOtp("123456"), OtpExpiresAt = DateTime.UtcNow.AddMinutes(-5) };
            _userRepo.GetQueryable().Returns(new List<User> { user }.AsQueryable().BuildMock());
            _messageService.Get("OtpExpired").Returns("OTP has expired.");

            var result = await _sut.VerifyOtpAsync(new VerifyOtpRequest { Email = "user@test.com", OtpCode = "123456" });

            Assert.False(result.IsSuccess);
            Assert.Equal(400, result.StatusCode);
            Assert.Equal("OTP has expired.", result.Message);
        }

        [Fact]
        public async Task VerifyOtpAsync_ReturnsSuccess_WhenOtpIsValid()
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "user@test.com",
                OtpHash = NFC.Platform.BuildingBlocks.Common.Helpers.OtpHasher.HashOtp("123456"),
                OtpExpiresAt = DateTime.UtcNow.AddMinutes(5),
                IsEmailVerified = false
            };
            _userRepo.GetQueryable().Returns(new List<User> { user }.AsQueryable().BuildMock());
            _userRoleRepo.FindAsync(Arg.Any<Expression<Func<UserRole, bool>>>()).Returns(new List<UserRole>());
            _roleRepo.FindAsync(Arg.Any<Expression<Func<Role, bool>>>()).Returns(new List<Role>());
            _tokenService.GenerateToken(user.Id, user.Email, Arg.Any<IEnumerable<string>>(), Arg.Any<Guid>(), Arg.Any<Guid?>(), Arg.Any<string>()).Returns("valid-jwt-token");
            _messageService.Get("OtpVerifiedSuccess").Returns("OTP verified successfully.");

            var result = await _sut.VerifyOtpAsync(new VerifyOtpRequest { Email = "user@test.com", OtpCode = "123456" });

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal("valid-jwt-token", result.Data.Token);
            Assert.True(user.IsEmailVerified);
            Assert.Null(user.OtpHash);
            Assert.Null(user.OtpExpiresAt);
            await _unitOfWork.Received(2).SaveChangesAsync();
        }
    }
}
