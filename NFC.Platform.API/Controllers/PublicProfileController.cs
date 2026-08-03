using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NFC.Platform.Application.DTOs.Card;
using NFC.Platform.Application.Interfaces.Services;

namespace NFC.Platform.API.Controllers
{
    [ApiController]
    [Route("api/public")]
    [AllowAnonymous]
    public class PublicProfileController(
        IProfileMetricService profileMetricService,
        IQrCodeService qrCodeService,
        IVCardService vCardService) : ControllerBase
    {
        private readonly IProfileMetricService _profileMetricService = profileMetricService ?? throw new ArgumentNullException(nameof(profileMetricService));
        private readonly IQrCodeService _qrCodeService = qrCodeService ?? throw new ArgumentNullException(nameof(qrCodeService));
        private readonly IVCardService _vCardService = vCardService ?? throw new ArgumentNullException(nameof(vCardService));

        /// <summary>
        /// Resolves a digital profile by its unique Id and returns the public profile data.
        /// </summary>
        [HttpGet("profile/{id:guid}")]
        [EnableRateLimiting("ResolvePublicProfilePolicy")]
        public async Task<IActionResult> ResolvePublicProfile([FromRoute] Guid id)
        {
            var result = await _profileMetricService.ResolvePublicProfileAsync(id);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Generates and downloads a vCard (.vcf) contact file for the specified profile GUID.
        /// Automatically logs a ContactSaved interaction metric.
        /// GET /api/public/profile/{id:guid}/vcard
        /// </summary>
        [HttpGet("profile/{id:guid}/vcard")]
        [EnableRateLimiting("ResolvePublicProfilePolicy")]
        public async Task<IActionResult> DownloadVCardById([FromRoute] Guid id)
        {
            var profileResult = await _profileMetricService.ResolvePublicProfileAsync(id);
            if (!profileResult.IsSuccess || profileResult.Data == null)
            {
                return StatusCode(profileResult.StatusCode, profileResult);
            }

            var vcardBytes = _vCardService.BuildVCardBytes(profileResult.Data);

            // Fire-and-forget background metric tracking
            _ = _profileMetricService.RecordMetricAsync(id, new RecordMetricRequest
            {
                InteractionType = NFC.Platform.Domain.Enums.InteractionType.ContactSaved
            });

            var filename = !string.IsNullOrWhiteSpace(profileResult.Data.FullName)
                ? $"{NFC.Platform.Application.Extensions.SubdomainHelper.Slugify(profileResult.Data.FullName)}.vcf"
                : "contact.vcf";

            return File(vcardBytes, "text/vcard; charset=utf-8", filename);
        }

        /// <summary>
        /// Generates a PNG QR Code encoding the profile's public URL by GUID.
        /// GET /api/public/profile/{id:guid}/qr?download=false
        /// </summary>
        [HttpGet("profile/{id:guid}/qr")]
        [EnableRateLimiting("ResolvePublicProfilePolicy")]
        [ResponseCache(Duration = 86400, Location = ResponseCacheLocation.Any)]
        public async Task<IActionResult> GetProfileQrById(
            [FromRoute] Guid id,
            [FromQuery] bool download = false)
        {
            var profileResult = await _profileMetricService.ResolvePublicProfileAsync(id);
            if (!profileResult.IsSuccess || string.IsNullOrWhiteSpace(profileResult.Data?.ProfileUrl))
            {
                return StatusCode(profileResult.StatusCode, profileResult);
            }

            var qrBytes = _qrCodeService.GeneratePngQrCode(profileResult.Data.ProfileUrl);
            Response.Headers.Append("Cache-Control", "public, max-age=86400");

            if (download)
            {
                var filename = !string.IsNullOrWhiteSpace(profileResult.Data.FullName)
                    ? $"{NFC.Platform.Application.Extensions.SubdomainHelper.Slugify(profileResult.Data.FullName)}-qr.png"
                    : "profile-qr.png";

                return File(qrBytes, "image/png", filename);
            }

            return File(qrBytes, "image/png");
        }

        /// <summary>
        /// Resolves a digital profile by its human-readable subdomain slug.
        /// Example: GET /api/public/profile/u/ahmed-ali
        /// </summary>
        [HttpGet("profile/u/{subdomain}")]
        [EnableRateLimiting("ResolvePublicProfilePolicy")]
        public async Task<IActionResult> ResolvePublicProfileBySubdomain([FromRoute] string subdomain)
        {
            var result = await _profileMetricService.ResolvePublicProfileBySubdomainAsync(subdomain);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Generates and downloads a vCard (.vcf) contact file for the specified subdomain slug.
        /// Automatically logs a ContactSaved interaction metric.
        /// GET /api/public/profile/u/{subdomain}/vcard
        /// </summary>
        [HttpGet("profile/u/{subdomain}/vcard")]
        [EnableRateLimiting("ResolvePublicProfilePolicy")]
        public async Task<IActionResult> DownloadVCardBySubdomain([FromRoute] string subdomain)
        {
            var profileResult = await _profileMetricService.ResolvePublicProfileBySubdomainAsync(subdomain);
            if (!profileResult.IsSuccess || profileResult.Data == null)
            {
                return StatusCode(profileResult.StatusCode, profileResult);
            }

            var vcardBytes = _vCardService.BuildVCardBytes(profileResult.Data);

            // Fire-and-forget background metric tracking
            _ = _profileMetricService.RecordMetricAsync(profileResult.Data.ProfileId, new RecordMetricRequest
            {
                InteractionType = NFC.Platform.Domain.Enums.InteractionType.ContactSaved
            });

            var filename = !string.IsNullOrWhiteSpace(subdomain) ? $"{subdomain}.vcf" : "contact.vcf";
            return File(vcardBytes, "text/vcard; charset=utf-8", filename);
        }

        /// <summary>
        /// Generates a PNG QR Code encoding the profile's public URL by subdomain slug.
        /// GET /api/public/profile/u/{subdomain}/qr?download=false
        /// </summary>
        [HttpGet("profile/u/{subdomain}/qr")]
        [EnableRateLimiting("ResolvePublicProfilePolicy")]
        [ResponseCache(Duration = 86400, Location = ResponseCacheLocation.Any)]
        public async Task<IActionResult> GetProfileQrBySubdomain(
            [FromRoute] string subdomain,
            [FromQuery] bool download = false)
        {
            var profileResult = await _profileMetricService.ResolvePublicProfileBySubdomainAsync(subdomain);
            if (!profileResult.IsSuccess || string.IsNullOrWhiteSpace(profileResult.Data?.ProfileUrl))
            {
                return StatusCode(profileResult.StatusCode, profileResult);
            }

            var qrBytes = _qrCodeService.GeneratePngQrCode(profileResult.Data.ProfileUrl);
            Response.Headers.Append("Cache-Control", "public, max-age=86400");

            if (download)
            {
                return File(qrBytes, "image/png", $"{subdomain}-qr.png");
            }

            return File(qrBytes, "image/png");
        }

        /// <summary>
        /// Records an interaction metric (view, save contact, link click) for a user profile anonymously.
        /// </summary>
        [HttpPost("profiles/{profileId:guid}/metrics")]
        public async Task<IActionResult> RecordMetric([FromRoute] Guid profileId, [FromBody] RecordMetricRequest request)
        {
            var result = await _profileMetricService.RecordMetricAsync(profileId, request);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }

            return Ok(result);
        }
    }
}
