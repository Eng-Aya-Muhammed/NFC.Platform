using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NFC.Platform.API.Controllers;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Application.DTOs.CardOrder;
using NFC.Platform.Application.Interfaces.Services;
using NFC.Platform.BuildingBlocks.Common.Constants;
using NFC.Platform.BuildingBlocks.Results;
using NSubstitute;
using Xunit;

namespace NFC.Platform.Tests.Controllers
{
    public class CardControllerTests
    {
        private readonly ICardService _cardService;
        private readonly ICardOrderService _cardOrderService;
        private readonly CardController _sut;

        public CardControllerTests()
        {
            _cardService = Substitute.For<ICardService>();
            _cardOrderService = Substitute.For<ICardOrderService>();
            _sut = new CardController(_cardService, _cardOrderService);
        }

        [Fact]
        public void CardController_ShouldHaveApiControllerAttribute()
        {
            var type = typeof(CardController);
            var attributes = type.GetCustomAttributes(typeof(ApiControllerAttribute), true);
            Assert.NotEmpty(attributes);
        }

        [Theory]
        [InlineData(nameof(CardController.GetById), null)]
        [InlineData(nameof(CardController.GetPaged), null)]
        [InlineData(nameof(CardController.Activate), null)]
        [InlineData(nameof(CardController.Create), AppPolicies.AdminOnly)]
        [InlineData(nameof(CardController.GetCardsForEncoding), AppPolicies.AdminOnly)]
        [InlineData(nameof(CardController.MarkEncoded), AppPolicies.AdminOnly)]
        [InlineData(nameof(CardController.ActivateById), AppPolicies.AdminOnly)]
        [InlineData(nameof(CardController.ActivateAllForOrder), AppPolicies.AdminOnly)]
        [InlineData(nameof(CardController.Deactivate), AppPolicies.AdminOnly)]
        [InlineData(nameof(CardController.Delete), AppPolicies.AdminOnly)]
        public void CardController_Endpoints_ShouldHaveCorrectAuthorizePolicy(string methodName, string? expectedPolicy)
        {
            var type = typeof(CardController);
            var methods = type.GetMethods().Where(m => m.Name == methodName).ToList();
            Assert.NotEmpty(methods);

            foreach (var method in methods)
            {
                var authorizeAttribute = method.GetCustomAttributes(typeof(AuthorizeAttribute), true)
                    .Cast<AuthorizeAttribute>()
                    .FirstOrDefault();

                Assert.NotNull(authorizeAttribute);
                Assert.Equal(expectedPolicy, authorizeAttribute.Policy);
            }
        }

        [Fact]
        public async Task GetById_CallsCardService_AndReturnsOk_OnSuccess()
        {
            var id = Guid.NewGuid();
            var cardDto = new CardDto { Id = id };
            _cardService.GetByIdAsync(id).Returns(ServiceResult<CardDto>.Success(cardDto));

            var result = await _sut.GetById(id) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            await _cardService.Received(1).GetByIdAsync(id);
        }

        [Fact]
        public async Task GetById_ReturnsErrorStatusCode_OnFailure()
        {
            var id = Guid.NewGuid();
            _cardService.GetByIdAsync(id).Returns(ServiceResult<CardDto>.Fail("Not found", 404));

            var result = await _sut.GetById(id) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task GetPaged_CallsCardService_AndReturnsOk()
        {
            var request = new PaginationRequest();
            var pagedResult = PagedResult<CardDto>.Create(new List<CardDto>(), 0, 1, 10);
            _cardService.GetPagedCardsAsync(request).Returns(ServiceResult<PagedResult<CardDto>>.Success(pagedResult));

            var result = await _sut.GetPaged(request) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            await _cardService.Received(1).GetPagedCardsAsync(request);
        }

        [Fact]
        public async Task Create_CallsCardService_AndReturnsStatusCode()
        {
            var request = new CreateCardRequest { ActivationCode = "ABC" };
            var cardDto = new CardDto { ActivationCode = "ABC" };
            _cardService.CreateCardAsync(request).Returns(new TestServiceResult<CardDto> { IsSuccess = true, Data = cardDto, StatusCode = 201 });

            var result = await _sut.Create(request) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(201, result.StatusCode);
            await _cardService.Received(1).CreateCardAsync(request);
        }

        [Fact]
        public async Task Create_ReturnsErrorStatusCode_OnFailure()
        {
            var request = new CreateCardRequest();
            _cardService.CreateCardAsync(request).Returns(ServiceResult<CardDto>.Fail("Already exists", 400));

            var result = await _sut.Create(request) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public async Task Activate_CallsCardService_AndReturnsOk_OnSuccess()
        {
            var request = new ActivateCardRequest { ActivationCode = "ABC" };
            var cardDto = new CardDto { ActivationCode = "ABC" };
            _cardService.ActivateCardAsync(request).Returns(ServiceResult<CardDto>.Success(cardDto));

            var result = await _sut.Activate(request) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            await _cardService.Received(1).ActivateCardAsync(request);
        }

        [Fact]
        public async Task Activate_ReturnsErrorStatusCode_OnFailure()
        {
            var request = new ActivateCardRequest();
            _cardService.ActivateCardAsync(request).Returns(ServiceResult<CardDto>.Fail("Invalid code", 400));

            var result = await _sut.Activate(request) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public async Task GetCardsForEncoding_CallsCardService_AndReturnsOk_OnSuccess()
        {
            var orderId = Guid.NewGuid();
            var list = new List<CardDto>();
            _cardService.GetCardsForEncodingAsync(orderId, "Encoded").Returns(ServiceResult<List<CardDto>>.Success(list));

            var result = await _sut.GetCardsForEncoding(orderId, "Encoded") as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            await _cardService.Received(1).GetCardsForEncodingAsync(orderId, "Encoded");
        }

        [Fact]
        public async Task GetCardsForEncoding_ReturnsErrorStatusCode_OnFailure()
        {
            var orderId = Guid.NewGuid();
            _cardService.GetCardsForEncodingAsync(orderId, null).Returns(ServiceResult<List<CardDto>>.Fail("Error", 500));

            var result = await _sut.GetCardsForEncoding(orderId, null) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(500, result.StatusCode);
        }

        [Fact]
        public async Task MarkEncoded_CallsCardService_AndReturnsOk_OnSuccess()
        {
            var id = Guid.NewGuid();
            _cardService.MarkCardEncodedAsync(id).Returns(ServiceResult.Success());

            var result = await _sut.MarkEncoded(id) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            await _cardService.Received(1).MarkCardEncodedAsync(id);
        }

        [Fact]
        public async Task MarkEncoded_ReturnsErrorStatusCode_OnFailure()
        {
            var id = Guid.NewGuid();
            _cardService.MarkCardEncodedAsync(id).Returns(ServiceResult.Fail("Error", 400));

            var result = await _sut.MarkEncoded(id) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public async Task ActivateById_CallsCardService_AndReturnsOk_OnSuccess()
        {
            var id = Guid.NewGuid();
            _cardService.ActivateCardByIdAsync(id).Returns(ServiceResult.Success());

            var result = await _sut.ActivateById(id) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            await _cardService.Received(1).ActivateCardByIdAsync(id);
        }

        [Fact]
        public async Task ActivateById_ReturnsErrorStatusCode_OnFailure()
        {
            var id = Guid.NewGuid();
            _cardService.ActivateCardByIdAsync(id).Returns(ServiceResult.Fail("Error", 400));

            var result = await _sut.ActivateById(id) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public async Task ActivateAllForOrder_CallsCardService_AndReturnsOk_OnSuccess()
        {
            var orderId = Guid.NewGuid();
            _cardService.ActivateAllCardsForOrderAsync(orderId).Returns(ServiceResult.Success());

            var result = await _sut.ActivateAllForOrder(orderId) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            await _cardService.Received(1).ActivateAllCardsForOrderAsync(orderId);
        }

        [Fact]
        public async Task ActivateAllForOrder_ReturnsErrorStatusCode_OnFailure()
        {
            var orderId = Guid.NewGuid();
            _cardService.ActivateAllCardsForOrderAsync(orderId).Returns(ServiceResult.Fail("Error", 400));

            var result = await _sut.ActivateAllForOrder(orderId) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public async Task Deactivate_CallsCardService_AndReturnsOk_OnSuccess()
        {
            var id = Guid.NewGuid();
            _cardService.DeactivateCardAsync(id).Returns(ServiceResult.Success());

            var result = await _sut.Deactivate(id) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            await _cardService.Received(1).DeactivateCardAsync(id);
        }

        [Fact]
        public async Task Deactivate_ReturnsErrorStatusCode_OnFailure()
        {
            var id = Guid.NewGuid();
            _cardService.DeactivateCardAsync(id).Returns(ServiceResult.Fail("Error", 400));

            var result = await _sut.Deactivate(id) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public async Task Delete_CallsCardService_AndReturnsOk_OnSuccess()
        {
            var id = Guid.NewGuid();
            _cardService.DeleteCardAsync(id).Returns(ServiceResult.Success());

            var result = await _sut.Delete(id) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            await _cardService.Received(1).DeleteCardAsync(id);
        }

        [Fact]
        public async Task Delete_ReturnsErrorStatusCode_OnFailure()
        {
            var id = Guid.NewGuid();
            _cardService.DeleteCardAsync(id).Returns(ServiceResult.Fail("Error", 400));

            var result = await _sut.Delete(id) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public async Task Reissue_CallsCardOrderService_AndReturnsOk_OnSuccess()
        {
            var id = Guid.NewGuid();
            var request = new ReissueCardRequest();
            var cardOrderDto = new CardOrderDto { Id = Guid.NewGuid() };
            _cardOrderService.ReissueCardAsync(id, request).Returns(ServiceResult<CardOrderDto>.Success(cardOrderDto));

            var result = await _sut.Reissue(id, request) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            await _cardOrderService.Received(1).ReissueCardAsync(id, request);
        }

        [Fact]
        public async Task Reissue_ReturnsErrorStatusCode_OnFailure()
        {
            var id = Guid.NewGuid();
            var request = new ReissueCardRequest();
            _cardOrderService.ReissueCardAsync(id, request).Returns(ServiceResult<CardOrderDto>.Fail("Error", 400));

            var result = await _sut.Reissue(id, request) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public void Activate_ShouldHaveCardActivationRateLimitingPolicy()
        {
            var methods = typeof(CardController).GetMethods()
                .Where(m => m.Name == nameof(CardController.Activate) && m.GetParameters().Length == 1
                    && m.GetParameters()[0].ParameterType == typeof(ActivateCardRequest))
                .ToList();

            Assert.NotEmpty(methods);
            var method = methods.First();

            var attr = method.GetCustomAttributes(typeof(EnableRateLimitingAttribute), true)
                .Cast<EnableRateLimitingAttribute>()
                .FirstOrDefault();

            Assert.NotNull(attr);
            Assert.Equal("CardActivationPolicy", attr.PolicyName);
        }

        [Theory]
        [InlineData(nameof(CardController.GetById))]
        [InlineData(nameof(CardController.GetPaged))]
        [InlineData(nameof(CardController.Create))]
        [InlineData(nameof(CardController.GetCardsForEncoding))]
        [InlineData(nameof(CardController.MarkEncoded))]
        [InlineData(nameof(CardController.ActivateAllForOrder))]
        [InlineData(nameof(CardController.Deactivate))]
        [InlineData(nameof(CardController.Delete))]
        [InlineData(nameof(CardController.Reissue))]
        public void NonSensitiveCardEndpoints_ShouldNotHaveRateLimiting(string methodName)
        {
            var method = typeof(CardController).GetMethods().FirstOrDefault(m => m.Name == methodName);
            Assert.NotNull(method);

            var attr = method.GetCustomAttributes(typeof(EnableRateLimitingAttribute), true);
            Assert.Empty(attr);
        }
    }
}
