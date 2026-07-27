namespace NFC.Platform.API.Controllers;

[ApiController]
[Route("api/card-packages")]
[Authorize]
public class CardPackageController(ICardPackageService cardPackageService) : ControllerBase
{
    private readonly ICardPackageService _cardPackageService = cardPackageService ?? throw new ArgumentNullException(nameof(cardPackageService));

    /// <summary>
    /// Returns all active card packages for user selection.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetActiveCardPackages()
    {
        var result = await _cardPackageService.GetActiveCardPackagesAsync();
        return Ok(result);
    }
}
