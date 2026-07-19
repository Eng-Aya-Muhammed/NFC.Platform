using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NFC.Platform.API.Controllers;
using NFC.Platform.Application.DTOs.Auth;
using NFC.Platform.Application.Interfaces.Services;
using NFC.Platform.BuildingBlocks.Common.Constants;
using NFC.Platform.BuildingBlocks.Results;
using NSubstitute;
using Xunit;

namespace NFC.Platform.Tests.Controllers
{
    public class AuthControllerTests
    {
        private readonly IAuthService _authService;
        private readonly AuthController _sut;

        public AuthControllerTests()
        {
            _authService = Substitute.For<IAuthService>();
            _sut = new AuthController(_authService);
        }

        [Fact]
        public void AuthController_ShouldHaveApiControllerAndRouteAttributes()
        {
            var type = typeof(AuthController);
            Assert.NotEmpty(type.GetCustomAttributes(typeof(ApiControllerAttribute), true));
            var route = type.GetCustomAttributes(typeof(RouteAttribute), true).Cast<RouteAttribute>().First();
            Assert.Equal("api/auth", route.Template);
        }

        [Fact]
        public async Task Login_CallsService_AndReturnsOk_OnSuccess()
        {
            var request = new LoginRequest();
            var dto = new AuthDto();
            _authService.LoginAsync(request).Returns(ServiceResult<AuthDto>.Success(dto));

            var result = await _sut.Login(request) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            await _authService.Received(1).LoginAsync(request);
        }

        [Fact]
        public async Task Login_ReturnsError_OnFailure()
        {
            var request = new LoginRequest();
            _authService.LoginAsync(request).Returns(ServiceResult<AuthDto>.Fail("Error", 400));

            var result = await _sut.Login(request) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public async Task Register_CallsService_AndReturnsOk_OnSuccess()
        {
            var request = new RegisterRequest();
            var dto = new AuthDto();
            _authService.RegisterAsync(request).Returns(ServiceResult<AuthDto>.Success(dto));

            var result = await _sut.Register(request) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            await _authService.Received(1).RegisterAsync(request);
        }

        [Fact]
        public async Task Register_ReturnsError_OnFailure()
        {
            var request = new RegisterRequest();
            _authService.RegisterAsync(request).Returns(ServiceResult<AuthDto>.Fail("Error", 400));

            var result = await _sut.Register(request) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public async Task Refresh_CallsService_AndReturnsOk_OnSuccess()
        {
            var request = new RefreshTokenRequest();
            var dto = new AuthDto();
            _authService.RefreshTokenAsync(request).Returns(ServiceResult<AuthDto>.Success(dto));

            var result = await _sut.Refresh(request) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            await _authService.Received(1).RefreshTokenAsync(request);
        }

        [Fact]
        public async Task Refresh_ReturnsError_OnFailure()
        {
            var request = new RefreshTokenRequest();
            _authService.RefreshTokenAsync(request).Returns(ServiceResult<AuthDto>.Fail("Error", 400));

            var result = await _sut.Refresh(request) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public async Task Revoke_CallsService_AndReturnsOk_OnSuccess()
        {
            var request = new RefreshTokenRequest();
            _authService.RevokeTokenAsync(request).Returns(ServiceResult.Success());

            var result = await _sut.Revoke(request) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            await _authService.Received(1).RevokeTokenAsync(request);
        }

        [Fact]
        public async Task Revoke_ReturnsError_OnFailure()
        {
            var request = new RefreshTokenRequest();
            _authService.RevokeTokenAsync(request).Returns(ServiceResult.Fail("Error", 400));

            var result = await _sut.Revoke(request) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public async Task ForgotPassword_CallsService_AndReturnsOk_OnSuccess()
        {
            var request = new ForgotPasswordRequest();
            _authService.ForgotPasswordAsync(request).Returns(ServiceResult.Success());

            var result = await _sut.ForgotPassword(request) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            await _authService.Received(1).ForgotPasswordAsync(request);
        }

        [Fact]
        public async Task ForgotPassword_ReturnsError_OnFailure()
        {
            var request = new ForgotPasswordRequest();
            _authService.ForgotPasswordAsync(request).Returns(ServiceResult.Fail("Error", 400));

            var result = await _sut.ForgotPassword(request) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public async Task ResetPassword_CallsService_AndReturnsOk_OnSuccess()
        {
            var request = new ResetPasswordRequest();
            _authService.ResetPasswordAsync(request).Returns(ServiceResult.Success());

            var result = await _sut.ResetPassword(request) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            await _authService.Received(1).ResetPasswordAsync(request);
        }

        [Fact]
        public async Task ResetPassword_ReturnsError_OnFailure()
        {
            var request = new ResetPasswordRequest();
            _authService.ResetPasswordAsync(request).Returns(ServiceResult.Fail("Error", 400));

            var result = await _sut.ResetPassword(request) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public async Task CreateUserByAdmin_CallsService_AndReturnsOk_OnSuccess()
        {
            var request = new AdminCreateUserRequest();
            var dto = new UserDto();
            _authService.CreateUserByAdminAsync(request).Returns(ServiceResult<UserDto>.Success(dto));

            var result = await _sut.CreateUserByAdmin(request) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            await _authService.Received(1).CreateUserByAdminAsync(request);
        }

        [Fact]
        public async Task CreateUserByAdmin_ReturnsError_OnFailure()
        {
            var request = new AdminCreateUserRequest();
            _authService.CreateUserByAdminAsync(request).Returns(ServiceResult<UserDto>.Fail("Error", 400));

            var result = await _sut.CreateUserByAdmin(request) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public void CreateUserByAdmin_ShouldHaveAuthorizeAttributeWithAdminOnlyPolicy()
        {
            var type = typeof(AuthController);
            var method = type.GetMethod(nameof(AuthController.CreateUserByAdmin));
            Assert.NotNull(method);

            var auth = method.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>().FirstOrDefault();
            Assert.NotNull(auth);
            Assert.Equal(AppPolicies.AdminOnly, auth.Policy);
        }

        [Theory]
        [InlineData(nameof(AuthController.Login), "LoginPolicy")]
        [InlineData(nameof(AuthController.Register), "RegisterPolicy")]
        [InlineData(nameof(AuthController.ResetPassword), "ResetPasswordPolicy")]
        [InlineData(nameof(AuthController.ForgotPassword), "ForgotPasswordPolicy")]
        public void AuthEndpoints_ShouldHaveCorrectRateLimitingPolicy(string methodName, string expectedPolicy)
        {
            var method = typeof(AuthController).GetMethod(methodName);
            Assert.NotNull(method);

            var attr = method.GetCustomAttributes(typeof(EnableRateLimitingAttribute), true)
                .Cast<EnableRateLimitingAttribute>()
                .FirstOrDefault();

            Assert.NotNull(attr);
            Assert.Equal(expectedPolicy, attr.PolicyName);
        }

        [Theory]
        [InlineData(nameof(AuthController.Refresh))]
        [InlineData(nameof(AuthController.Revoke))]
        [InlineData(nameof(AuthController.CreateUserByAdmin))]
        public void NonSensitiveEndpoints_ShouldNotHaveRateLimiting(string methodName)
        {
            var method = typeof(AuthController).GetMethod(methodName);
            Assert.NotNull(method);

            var attr = method.GetCustomAttributes(typeof(EnableRateLimitingAttribute), true);
            Assert.Empty(attr);
        }
    }
}
