using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NFC.Platform.Application.Interfaces.Services;
using System;
using System.Threading.Tasks;
using NFC.Platform.BuildingBlocks.Common.Helpers;

namespace NFC.Platform.API.Controllers
{
    [ApiController]
    [Route("api/templates")]
    [Authorize]
    public class CardTemplateController(
        ICardTemplateService cardTemplateService) : ControllerBase
    {
        private readonly ICardTemplateService _cardTemplateService = cardTemplateService ?? throw new ArgumentNullException(nameof(cardTemplateService));

        [HttpGet]
        public async Task<IActionResult> GetActiveTemplates()
        {
            var result = await _cardTemplateService.GetActiveTemplatesAsync();
            return Ok(result);
        }
    }
}
