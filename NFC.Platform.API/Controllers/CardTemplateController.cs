using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Application.Interfaces.Services;
using NFC.Platform.BuildingBlocks.Common.Helpers;

namespace NFC.Platform.API.Controllers
{
    [ApiController]
    [Route("api/templates")]
    [Authorize]
    public class CardTemplateController : ControllerBase
    {
        private readonly ICardTemplateService _cardTemplateService;
        private readonly ICurrentTenant _currentTenant;

        public CardTemplateController(
            ICardTemplateService cardTemplateService,
            ICurrentTenant currentTenant)
        {
            _cardTemplateService = cardTemplateService ?? throw new ArgumentNullException(nameof(cardTemplateService));
            _currentTenant = currentTenant ?? throw new ArgumentNullException(nameof(currentTenant));
        }

        [HttpGet]
        public async Task<IActionResult> GetActiveTemplates()
        {
            var result = await _cardTemplateService.GetActiveTemplatesAsync();
            return Ok(result);
        }

        [HttpPost("select")]
        public async Task<IActionResult> SelectTemplate([FromBody] SelectTemplateRequest request)
        {
            var userId = _currentTenant.UserId;
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            var result = await _cardTemplateService.SelectTemplateAsync(userId.Value, request.TemplateId);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result);
            }
            return Ok(result);
        }
    }
}
