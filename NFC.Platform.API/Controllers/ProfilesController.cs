using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NFC.Platform.Application.DTOs.Employee;
using NFC.Platform.Application.DTOs.Profile;
using NFC.Platform.Application.Interfaces.Services;
using NFC.Platform.BuildingBlocks.Common.Helpers;

namespace NFC.Platform.API.Controllers
{
    [ApiController]
    [Route("api/user/profile")]
    [Authorize]
    public class ProfilesController(IProfileService profileService, ICurrentTenant currentTenant) : ControllerBase
    {
        private readonly IProfileService _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
        private readonly ICurrentTenant _currentTenant = currentTenant ?? throw new ArgumentNullException(nameof(currentTenant));

        /// <summary>
        /// Retrieves the profile details of the currently authenticated individual user.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            var userId = _currentTenant.UserId;
            if (!userId.HasValue)
            {
                return Unauthorized("User is not authenticated.");
            }

            var result = await _profileService.GetProfileAsync(userId.Value);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }
            return Ok(result);
        }

        /// <summary>
        /// Updates the digital profile info of the currently authenticated user.
        /// </summary>
        [HttpPut]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateMyProfileRequest request)
        {
            var userId = _currentTenant.UserId;
            if (!userId.HasValue)
            {
                return Unauthorized("User is not authenticated.");
            }

            var result = await _profileService.UpdateProfileAsync(userId.Value, request);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }
            return Ok(result);
        }

        /// <summary>
        /// Synchronizes the custom links collection for the currently authenticated user's digital profile.
        /// </summary>
        [HttpPut("links")]
        public async Task<IActionResult> SynchronizeLinks([FromBody] SynchronizeLinksRequest request)
        {
            var userId = _currentTenant.UserId;
            if (!userId.HasValue)
            {
                return Unauthorized("User is not authenticated.");
            }

            var result = await _profileService.SynchronizeLinksAsync(userId.Value, request);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Applies a specific digital card template to the authenticated user's public profile.
        /// </summary>
        [HttpPost("template/{templateId:guid}")]
        public async Task<IActionResult> ApplyPublicProfileTemplate([FromRoute] Guid templateId)
        {
            var userId = _currentTenant.UserId;
            if (!userId.HasValue) return Unauthorized("User is not authenticated.");
            var result = await _profileService.UpdateProfileTemplateAsync(userId.Value, templateId);
            if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
            return Ok(result);
        }

        /// <summary>
        /// Removes the specific digital card template from the authenticated user's public profile, reverting to the default.
        /// </summary>
        [HttpDelete("template")]
        public async Task<IActionResult> RemovePublicProfileTemplate()
        {
            var userId = _currentTenant.UserId;
            if (!userId.HasValue) return Unauthorized("User is not authenticated.");
            var result = await _profileService.UpdateProfileTemplateAsync(userId.Value, null);
            if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
            return Ok(result);
        }
    }
}
