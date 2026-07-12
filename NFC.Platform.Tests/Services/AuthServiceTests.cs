using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Application.Interfaces.Repositories;
using NFC.Platform.Application.Interfaces.Services;
using NFC.Platform.Application.Services;
using NFC.Platform.BuildingBlocks.Common.Constants;
using NFC.Platform.BuildingBlocks.Common.Helpers;
using NFC.Platform.BuildingBlocks.Localization;
using NFC.Platform.Domain.Entities;
using NFC.Platform.Domain.Enums;
using NSubstitute;
using Xunit;

namespace NFC.Platform.Tests.Services
{
    public class AuthServiceTests
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITokenService _tokenService;
        private readonly IMessageService _messageService;
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

            _userRepo = Substitute.For<IGenericRepository<User>>();
            _roleRepo = Substitute.For<IGenericRepository<Role>>();
            _userRoleRepo = Substitute.For<IGenericRepository<UserRole>>();
            _tokenRepo = Substitute.For<IGenericRepository<RefreshToken>>();

            _unitOfWork.Repository<User>().Returns(_userRepo);
            _unitOfWork.Repository<Role>().Returns(_roleRepo);
            _unitOfWork.Repository<UserRole>().Returns(_userRoleRepo);
            _unitOfWork.Repository<RefreshToken>().Returns(_tokenRepo);

            _sut = new AuthService(_unitOfWork, _tokenService, _messageService);
        }

        [Fact]
        public async Task LoginAsync_ReturnsUnauthorized_WhenUserDoesNotExist()
        {
            // Arrange
            var request = new LoginRequest { Email = "notfound@test.com", Password = "Password123!" };
            _userRepo.FindAsync(Arg.Any<Expression<Func<User, bool>>>())
                .Returns(new List<User>()); // Empty list
            _messageService.Get("InvalidCredentials").Returns("Invalid email or password.");

            // Act
            var result = await _sut.LoginAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(401, result.StatusCode);
            Assert.Equal("Invalid email or password.", result.Message);
        }

        [Fact]
        public async Task LoginAsync_ReturnsSuccess_WhenCredentialsAreValid()
        {
            // Arrange
            var password = "Password123!";
            var hashedPassword = PasswordHasher.HashPassword(password);
            var user = new User { Email = "user@test.com", PasswordHash = hashedPassword, Username = "testuser" };

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

            // Act
            var result = await _sut.LoginAsync(request);

            // Assert
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
            // Arrange
            var request = new RegisterRequest { Email = "exists@test.com", Username = "user", Password = "123" };
            var existingUser = new User { Email = "exists@test.com" };

            _userRepo.FindAsync(Arg.Any<Expression<Func<User, bool>>>())
                .Returns(new List<User> { existingUser });
            _messageService.Get("UserAlreadyExists").Returns("User already exists.");

            // Act
            var result = await _sut.RegisterAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(400, result.StatusCode);
            Assert.Equal("User already exists.", result.Message);
        }

        [Fact]
        public async Task RefreshTokenAsync_ReturnsUnauthorized_WhenTokenIsExpired()
        {
            // Arrange
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

            // Act
            var result = await _sut.RefreshTokenAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(401, result.StatusCode);
        }

        [Fact]
        public async Task ForgotPasswordAsync_UpdatesResetToken_WhenUserExists()
        {
            // Arrange
            var email = "user@test.com";
            var user = new User { Email = email };
            var request = new ForgotPasswordRequest { Email = email };

            _userRepo.FindAsync(Arg.Any<Expression<Func<User, bool>>>())
                .Returns(new List<User> { user });

            // Act
            var result = await _sut.ForgotPasswordAsync(request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(user.PasswordResetToken);
            Assert.NotNull(user.PasswordResetTokenExpires);
            await _unitOfWork.Received().SaveChangesAsync();
        }

        [Fact]
        public async Task ResetPasswordAsync_ChangesPassword_WhenTokenIsValid()
        {
            // Arrange
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

            // Act
            var result = await _sut.ResetPasswordAsync(request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Null(user.PasswordResetToken);
            Assert.Null(user.PasswordResetTokenExpires);
            Assert.True(PasswordHasher.VerifyPassword("NewSecurePassword123!", user.PasswordHash));
            await _unitOfWork.Received().SaveChangesAsync();
        }

        [Fact]
        public async Task RegisterAsync_ReturnsSuccess_WhenRequestIsValid()
        {
            // Arrange
            var request = new RegisterRequest
            {
                Email = "newuser@test.com",
                Username = "newuser",
                Password = "Password123!"
            };

            _userRepo.FindAsync(Arg.Any<Expression<Func<User, bool>>>())
                .Returns(new List<User>()); // User does not exist

            _roleRepo.FindAsync(Arg.Any<Expression<Func<Role, bool>>>())
                .Returns(new List<Role> { new() { Id = Guid.NewGuid(), Name = AppRole.Customer.ToString() } });

            _userRoleRepo.FindAsync(Arg.Any<Expression<Func<UserRole, bool>>>())
                .Returns(new List<UserRole>());

            _tokenService.GenerateToken(Arg.Any<Guid>(), request.Email, Arg.Any<IEnumerable<string>>(), Arg.Any<Guid>(), Arg.Any<Guid?>(), Arg.Any<string>())
                .Returns("mock-access-token");

            // Act
            var result = await _sut.RegisterAsync(request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal("mock-access-token", result.Data.Token);
            await _userRepo.Received(1).AddAsync(Arg.Any<User>());
            await _userRoleRepo.Received(1).AddAsync(Arg.Any<UserRole>());
            await _unitOfWork.Received(4).SaveChangesAsync(); // Saved four times: Tenant added, User added, UserRole added, and RefreshToken added
        }

        [Fact]
        public async Task RegisterAsync_CreatesCompanyAndAssignsRole_WhenAccountTypeIsCompanyAdmin()
        {
            // Arrange
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

            // Act
            var result = await _sut.RegisterAsync(request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal("mock-access-token", result.Data.Token);
            await _userRepo.Received(1).AddAsync(Arg.Any<User>());
            await companyRepo.Received(1).AddAsync(Arg.Any<Company>());
            await _userRoleRepo.Received(1).AddAsync(Arg.Any<UserRole>());
            await _unitOfWork.Received(6).SaveChangesAsync(); // Saved six times
        }

        [Fact]
        public async Task RefreshTokenAsync_ReturnsSuccess_WhenTokenIsValid()
        {
            // Arrange
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

            // Act
            var result = await _sut.RefreshTokenAsync(request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal("new-access-token", result.Data.Token);
            Assert.Equal("Token refreshed successfully.", result.Message);
            Assert.True(validRefreshToken.IsRevoked); // Old token should be marked revoked
            await _tokenRepo.Received(1).AddAsync(Arg.Any<RefreshToken>()); // New refresh token added
            await _unitOfWork.Received().SaveChangesAsync();
        }

        [Fact]
        public async Task RevokeTokenAsync_RevokesToken_WhenTokenExists()
        {
            // Arrange
            var token = new RefreshToken { Token = "token-to-revoke", IsRevoked = false };
            var request = new RefreshTokenRequest { RefreshToken = "token-to-revoke" };

            _tokenRepo.FindAsync(Arg.Any<Expression<Func<RefreshToken, bool>>>())
                .Returns(new List<RefreshToken> { token });

            // Act
            var result = await _sut.RevokeTokenAsync(request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(token.IsRevoked);
            await _unitOfWork.Received(1).SaveChangesAsync();
        }

        [Fact]
        public async Task ResetPasswordAsync_ReturnsBadRequest_WhenTokenIsExpired()
        {
            // Arrange
            var token = "expired-reset-token";
            var user = new User
            {
                Email = "user@test.com",
                PasswordResetToken = token,
                PasswordResetTokenExpires = DateTime.UtcNow.AddMinutes(-5) // Expired 5 minutes ago
            };
            var request = new ResetPasswordRequest
            {
                Token = token,
                NewPassword = "NewSecurePassword123!"
            };

            _userRepo.FindAsync(Arg.Any<Expression<Func<User, bool>>>())
                .Returns(new List<User> { user });

            _messageService.Get("ResetTokenExpired").Returns("Reset token has expired.");

            // Act
            var result = await _sut.ResetPasswordAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(400, result.StatusCode);
            Assert.Equal("Reset token has expired.", result.Message);
        }

        [Fact]
        public async Task ResetPasswordAsync_ReturnsBadRequest_WhenTokenDoesNotExist()
        {
            // Arrange
            var request = new ResetPasswordRequest
            {
                Token = "non-existent-token",
                NewPassword = "NewSecurePassword123!"
            };

            _userRepo.FindAsync(Arg.Any<Expression<Func<User, bool>>>())
                .Returns(new List<User>()); // Empty list
            _messageService.Get("InvalidResetToken").Returns("Invalid reset token.");

            // Act
            var result = await _sut.ResetPasswordAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(400, result.StatusCode);
            Assert.Equal("Invalid reset token.", result.Message);
        }

        [Fact]
        public async Task CreateUserByAdminAsync_ReturnsSuccess_WhenAdminCreatesValidUser()
        {
            // Arrange
            var request = new AdminCreateUserRequest
            {
                Email = "newadmin@test.com",
                Username = "newadmin",
                Password = "Password123!",
                Role = AppRole.Admin
            };

            _userRepo.FindAsync(Arg.Any<Expression<Func<User, bool>>>())
                .Returns(new List<User>()); // User does not exist

            _roleRepo.FindAsync(Arg.Any<Expression<Func<Role, bool>>>())
                .Returns(new List<Role> { new() { Id = Guid.NewGuid(), Name = AppRole.Admin.ToString() } });

            _userRoleRepo.FindAsync(Arg.Any<Expression<Func<UserRole, bool>>>())
                .Returns(new List<UserRole>());

            _messageService.Get("UserCreated").Returns("User created successfully.");

            // Act
            var result = await _sut.CreateUserByAdminAsync(request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal("newadmin", result.Data.Username);
            Assert.Equal(AppRole.Admin.ToString(), result.Data.Role);
            Assert.Equal("User created successfully.", result.Message);
            await _userRepo.Received(1).AddAsync(Arg.Any<User>());
            await _userRoleRepo.Received(1).AddAsync(Arg.Any<UserRole>());
            await _unitOfWork.Received(2).SaveChangesAsync();
        }

        [Fact]
        public async Task CreateUserByAdminAsync_ReturnsBadRequest_WhenUserAlreadyExists()
        {
            // Arrange
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

            // Act
            var result = await _sut.CreateUserByAdminAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(400, result.StatusCode);
            Assert.Equal("User already exists.", result.Message);
        }
    }
}
