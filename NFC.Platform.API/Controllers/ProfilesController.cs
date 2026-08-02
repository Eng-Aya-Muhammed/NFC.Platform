namespace NFC.Platform.API.Controllers;

[ApiController]
[Route("api/user/profile")]
[Authorize]
public class ProfilesController(IProfileService profileService, ICurrentTenant currentTenant, IMessageService msg) : ControllerBase
{
    private readonly IProfileService _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
    private readonly ICurrentTenant _currentTenant = currentTenant ?? throw new ArgumentNullException(nameof(currentTenant));
    private readonly IMessageService _msg = msg ?? throw new ArgumentNullException(nameof(msg));

    /// <summary>
    /// Retrieves the profile details of the currently authenticated individual user.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetProfile()
    {
        var userId = _currentTenant.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized(_msg.Get("UserNotAuthenticated"));
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
            return Unauthorized(_msg.Get("UserNotAuthenticated"));
        }

        if (request.PreferredLanguage.HasValue)
        {
            var culture = request.PreferredLanguage.Value == Domain.Enums.PreferredLanguage.English ? "en" : "ar";
            var cultureInfo = new System.Globalization.CultureInfo(culture);
            System.Globalization.CultureInfo.CurrentCulture = cultureInfo;
            System.Globalization.CultureInfo.CurrentUICulture = cultureInfo;
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
            return Unauthorized(_msg.Get("UserNotAuthenticated"));
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
    [HttpPost("apply-template/{templateId:guid}")]
    public async Task<IActionResult> ApplyPublicProfileTemplate([FromRoute] Guid templateId)
    {
        var userId = _currentTenant.UserId;
        if (!userId.HasValue) return Unauthorized(_msg.Get("UserNotAuthenticated"));
        var result = await _profileService.UpdateProfileTemplateAsync(userId.Value, templateId);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    /// <summary>
    /// Removes the applied digital card template from the authenticated user's public profile, reverting to default.
    /// </summary>
    [HttpDelete("remove-template")]
    public async Task<IActionResult> RemovePublicProfileTemplate()
    {
        var userId = _currentTenant.UserId;
        if (!userId.HasValue) return Unauthorized(_msg.Get("UserNotAuthenticated"));
        var result = await _profileService.UpdateProfileTemplateAsync(userId.Value, null);
        if (!result.IsSuccess) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }
}
