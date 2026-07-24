using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NFC.Platform.Application.DTOs.Company;
using NFC.Platform.Application.Interfaces.Services;
using NFC.Platform.Domain.Constants;
using NFC.Platform.Infrastructure.Authorization;

namespace NFC.Platform.API.Controllers
{
    [ApiController]
    [Route("api/company")]
    [HasPermission(AppPermissions.Company.View)]
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
        [HasPermission(AppPermissions.Company.Update)]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateCompanyProfileRequest request)
        {
            var result = await _companyService.UpdateCompanyProfileAsync(request);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }
            return Ok(result);
        }

        [HttpPost("change-password")]
        [EnableRateLimiting("ChangePasswordPolicy")]
        [HasPermission(AppPermissions.Company.Update)]
        public async Task<IActionResult> ChangePassword([FromBody] CompanyChangePasswordRequest request)
        {
            var result = await _companyService.ChangeCompanyAdminPasswordAsync(request);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }
            return Ok(result);
        }

        /// <summary>
        /// Applies a specific digital card template to the company's public profile (overrides employee defaults).
        /// </summary>
        [HttpPost("template/{templateId:guid}")]
        [HasPermission(AppPermissions.Company.Update)]
        public async Task<IActionResult> ApplyCompanyPublicProfileTemplate([FromRoute] Guid templateId)
        {
            var result = await _companyService.UpdateCompanyTemplateAsync(templateId);
            if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
            return Ok(result);
        }

        /// <summary>
        /// Removes the specific digital card template from the company's public profile.
        /// </summary>
        [HttpDelete("template")]
        [HasPermission(AppPermissions.Company.Update)]
        public async Task<IActionResult> RemoveCompanyPublicProfileTemplate()
        {
            var result = await _companyService.UpdateCompanyTemplateAsync(null);
            if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
            return Ok(result);
        }
    }
}

