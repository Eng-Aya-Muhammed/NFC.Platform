using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NFC.Platform.API.Controllers;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Application.Interfaces.Services;
using NFC.Platform.BuildingBlocks.Results;
using NSubstitute;
using Xunit;

namespace NFC.Platform.Tests.Controllers
{
    public class CardTemplateControllerTests
    {
        private readonly ICardTemplateService _cardTemplateService;
        private readonly CardTemplateController _sut;

        public CardTemplateControllerTests()
        {
            _cardTemplateService = Substitute.For<ICardTemplateService>();
            _sut = new CardTemplateController(_cardTemplateService);
        }

        [Fact]
        public async Task GetActiveTemplates_CallsCardTemplateService_AndReturnsOk()
        {
            var dtos = new List<CardTemplateDto>();
            _cardTemplateService.GetActiveTemplatesAsync().Returns(ServiceResult<IReadOnlyList<CardTemplateDto>>.Success(dtos));

            var result = await _sut.GetActiveTemplates() as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            await _cardTemplateService.Received(1).GetActiveTemplatesAsync();
        }
    }
}
