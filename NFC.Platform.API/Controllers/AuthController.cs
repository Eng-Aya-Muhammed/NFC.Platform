using Microsoft.AspNetCore.RateLimiting;

namespace NFC.Platform.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController(IAuthService authService) : ControllerBase
    {
        private readonly IAuthService _authService = authService ?? throw new ArgumentNullException(nameof(authService));

        [HttpPost("login")]
        [AllowAnonymous]
        [EnableRateLimiting("LoginPolicy")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var result = await _authService.LoginAsync(request);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }

            return Ok(result);
        }

        [HttpPost("register")]
        [AllowAnonymous]
        [EnableRateLimiting("RegisterPolicy")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var result = await _authService.RegisterAsync(request);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }

            return Ok(result);
        }

        [HttpPost("google-register")]
        [AllowAnonymous]
        [EnableRateLimiting("RegisterPolicy")]
        public async Task<IActionResult> RegisterWithGoogle([FromBody] GoogleRegisterRequest request)
        {
            var result = await _authService.RegisterWithGoogleAsync(request);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }

            return Ok(result);
        }

        [HttpPost("verify-otp")]
        [HttpPost("verify-whatsapp-otp")]
        [AllowAnonymous]
        [EnableRateLimiting("RegisterPolicy")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request)
        {
            var result = await _authService.VerifyOtpAsync(request);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }

            return Ok(result);
        }

        [HttpPost("resend-otp")]
        [HttpPost("resend-whatsapp-otp")]
        [AllowAnonymous]
        public async Task<IActionResult> ResendOtp([FromBody] ResendOtpRequest request)
        {
            var result = await _authService.ResendOtpAsync(request);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }

            return Ok(result);
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
        {
            var result = await _authService.RefreshTokenAsync(request);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }

            return Ok(result);
        }

        [HttpPost("revoke")]
        [AllowAnonymous]
        public async Task<IActionResult> Revoke([FromBody] RefreshTokenRequest request)
        {
            var result = await _authService.RevokeTokenAsync(request);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }

            return Ok(result);
        }

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        [EnableRateLimiting("ForgotPasswordPolicy")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            var result = await _authService.ForgotPasswordAsync(request);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }

            return Ok(result);
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        [EnableRateLimiting("ResetPasswordPolicy")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            var result = await _authService.ResetPasswordAsync(request);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }

            return Ok(result);
        }

        [HttpPost("admin/create-user")]
        [Authorize(Policy = AppPolicies.AdminOnly)]
        public async Task<IActionResult> CreateUserByAdmin([FromBody] AdminCreateUserRequest request)
        {
            var result = await _authService.CreateUserByAdminAsync(request);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }

            return Ok(result);
        }
    }
}
