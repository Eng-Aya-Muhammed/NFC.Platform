using Microsoft.AspNetCore.RateLimiting;

namespace NFC.Platform.API.Controllers
{
    [ApiController]
    [Route("api/company")]
    [Authorize(Policy = AppPolicies.CompanyAdminOnly)]
    public class CompanyController(ICompanyService companyService) : ControllerBase
    {
        private readonly ICompanyService _companyService = companyService ?? throw new ArgumentNullException(nameof(companyService));

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            var result = await _companyService.GetCompanyDashboardAsync();
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }
            return Ok(result);
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var result = await _companyService.GetMyCompanyProfileAsync();
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }
            return Ok(result);
        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateCompanyProfileRequest request)
        {
            var result = await _companyService.UpdateCompanyProfileAsync(request);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }
            return Ok(result);
        }

        /// <summary>
        /// Sets the company's digital profile template.
        /// Applies to all employee public profile pages at GET /c/{code}.
        /// </summary>
        [HttpPatch("template")]
        public async Task<IActionResult> UpdateTemplate([FromBody] UpdateCompanyTemplateRequest request)
        {
            var result = await _companyService.UpdateCompanyTemplateAsync(request);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }
            return Ok(result);
        }

        [HttpPost("change-password")]
        [EnableRateLimiting("ChangePasswordPolicy")]
        public async Task<IActionResult> ChangePassword([FromBody] CompanyChangePasswordRequest request)
        {
            var result = await _companyService.ChangeCompanyAdminPasswordAsync(request);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }
            return Ok(result);
        }
    }
}
