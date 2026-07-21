using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NFC.Platform.API.Controllers;
using NFC.Platform.API.Models;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Application.DTOs.CardOrder;
using NFC.Platform.Application.Interfaces.Services;
using NFC.Platform.BuildingBlocks.Localization;
using NFC.Platform.BuildingBlocks.Results;
using NFC.Platform.Domain.Entities;
using NFC.Platform.Domain.Enums;
using NSubstitute;
using Xunit;

namespace NFC.Platform.Tests.Controllers
{
    public class CardOrderControllerTests
    {
        private readonly ICardOrderService _cardOrderService;
        private readonly IMessageService _messageService;
        private readonly CardOrderController _sut;

        public CardOrderControllerTests()
        {
            _cardOrderService = Substitute.For<ICardOrderService>();
            _messageService = Substitute.For<IMessageService>();
            _sut = new CardOrderController(_cardOrderService, _messageService);
        }

        [Fact]
        public void CardOrderController_ShouldHaveAuthorizeAndRouteAttributes()
        {
            var type = typeof(CardOrderController);
            
            var authorizeAttributes = type.GetCustomAttributes(typeof(AuthorizeAttribute), true);
            Assert.NotEmpty(authorizeAttributes);

            var routeAttributes = type.GetCustomAttributes(typeof(RouteAttribute), true).Cast<RouteAttribute>().ToList();
            Assert.NotEmpty(routeAttributes);
            Assert.Equal("api/card-orders", routeAttributes.First().Template);
        }

        [Fact]
        public async Task GetPricing_CallsService_AndReturnsOk()
        {
            var pricing = new OrderPricingResponseDto();
            _cardOrderService.GetOrderPricingAsync("plastic", 5).Returns(ServiceResult<OrderPricingResponseDto>.Success(pricing));

            var result = await _sut.GetPricing("plastic", 5) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            await _cardOrderService.Received(1).GetOrderPricingAsync("plastic", 5);
        }

        [Fact]
        public async Task GetActivePricingCatalog_CallsService_AndReturnsOk()
        {
            var catalog = new List<CardPricingDto>();
            _cardOrderService.GetActivePricingCatalogAsync().Returns(ServiceResult<IReadOnlyList<CardPricingDto>>.Success(catalog));

            var result = await _sut.GetActivePricingCatalog() as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            await _cardOrderService.Received(1).GetActivePricingCatalogAsync();
        }

        [Fact]
        public async Task GetPaged_CallsService_AndReturnsOk()
        {
            var request = new PaginationRequest();
            var paged = PagedResult<CardOrderDto>.Create(new List<CardOrderDto>(), 0, 1, 10);
            _cardOrderService.GetPagedAsync(request, "PendingReview").Returns(ServiceResult<PagedResult<CardOrderDto>>.Success(paged));

            var result = await _sut.GetPaged(request, "PendingReview") as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            await _cardOrderService.Received(1).GetPagedAsync(request, "PendingReview");
        }

        [Fact]
        public async Task GetById_CallsService_AndReturnsOk_OnSuccess()
        {
            var id = Guid.NewGuid();
            var order = new CardOrderDto { Id = id };
            _cardOrderService.GetByIdAsync(id).Returns(ServiceResult<CardOrderDto>.Success(order));

            var result = await _sut.GetById(id) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            await _cardOrderService.Received(1).GetByIdAsync(id);
        }

        [Fact]
        public async Task GetById_ReturnsErrorStatusCode_OnFailure()
        {
            var id = Guid.NewGuid();
            _cardOrderService.GetByIdAsync(id).Returns(ServiceResult<CardOrderDto>.Fail("Not found", 404));

            var result = await _sut.GetById(id) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task Create_CallsService_AndReturnsStatusCode()
        {
            var request = new CreateCardOrderRequest();
            var dto = new CardOrderDto { Id = Guid.NewGuid() };
            _cardOrderService.CreateAsync(request).Returns(new TestServiceResult<CardOrderDto> { IsSuccess = true, Data = dto, StatusCode = 201 });

            var result = await _sut.Create(request) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(201, result.StatusCode);
            await _cardOrderService.Received(1).CreateAsync(request);
        }

        [Fact]
        public async Task Create_ReturnsErrorStatusCode_OnFailure()
        {
            var request = new CreateCardOrderRequest();
            _cardOrderService.CreateAsync(request).Returns(ServiceResult<CardOrderDto>.Fail("Error", 400));

            var result = await _sut.Create(request) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public async Task Reorder_CallsService_AndReturnsStatusCode()
        {
            var id = Guid.NewGuid();
            var request = new ReorderRequest();
            var dto = new CardOrderDto { Id = Guid.NewGuid() };
            _cardOrderService.CreateReorderAsync(id, request).Returns(new TestServiceResult<CardOrderDto> { IsSuccess = true, Data = dto, StatusCode = 201 });

            var result = await _sut.Reorder(id, request) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(201, result.StatusCode);
            await _cardOrderService.Received(1).CreateReorderAsync(id, request);
        }

        [Fact]
        public async Task Reorder_ReturnsErrorStatusCode_OnFailure()
        {
            var id = Guid.NewGuid();
            var request = new ReorderRequest();
            _cardOrderService.CreateReorderAsync(id, request).Returns(ServiceResult<CardOrderDto>.Fail("Error", 400));

            var result = await _sut.Reorder(id, request) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public async Task Delete_CallsService_AndReturnsOk_OnSuccess()
        {
            var id = Guid.NewGuid();
            _cardOrderService.DeleteAsync(id).Returns(ServiceResult.Success());

            var result = await _sut.Delete(id) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            await _cardOrderService.Received(1).DeleteAsync(id);
        }

        [Fact]
        public async Task Delete_ReturnsErrorStatusCode_OnFailure()
        {
            var id = Guid.NewGuid();
            _cardOrderService.DeleteAsync(id).Returns(ServiceResult.Fail("Error", 400));

            var result = await _sut.Delete(id) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public async Task GetEmployeesImportStatus_CallsService_AndReturnsOk_OnSuccess()
        {
            var id = Guid.NewGuid();
            var status = new EmployeesImportStatusDto();
            _cardOrderService.GetEmployeesImportStatusAsync(id).Returns(ServiceResult<EmployeesImportStatusDto>.Success(status));

            var result = await _sut.GetEmployeesImportStatus(id) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            await _cardOrderService.Received(1).GetEmployeesImportStatusAsync(id);
        }

        [Fact]
        public async Task GetEmployeesImportStatus_ReturnsErrorStatusCode_OnFailure()
        {
            var id = Guid.NewGuid();
            _cardOrderService.GetEmployeesImportStatusAsync(id).Returns(ServiceResult<EmployeesImportStatusDto>.Fail("Error", 404));

            var result = await _sut.GetEmployeesImportStatus(id) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task PlaceBulkOrderFromExcel_ReturnsBadRequest_WhenRequestOrFileIsNull()
        {
            _messageService.Get("NoFileUploaded").Returns("No file was uploaded.");

            var result = await _sut.PlaceBulkOrderFromExcel(null!) as BadRequestObjectResult;

            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
            Assert.Equal("No file was uploaded.", result.Value);
        }

        [Fact]
        public async Task PlaceBulkOrderFromExcel_CallsService_AndReturnsAccepted_OnSuccess()
        {
            var file = Substitute.For<IFormFile>();
            var request = new ImportEmployeesAndOrderCardsRequest
            {
                File = file,
                CardType = CardType.Plastic,
                CardDesignType = CardDesignType.NeedCustomDesign,
                Notes = "Design notes"
            };

            var job = new EmployeeImportJob();
            _cardOrderService.QueueEmployeeImportJobAsync(
                file,
                CardType.Plastic,
                CardDesignType.NeedCustomDesign,
                "Design notes").Returns(ServiceResult<EmployeeImportJob>.Success(job));

            var result = await _sut.PlaceBulkOrderFromExcel(request) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(202, result.StatusCode);
            await _cardOrderService.Received(1).QueueEmployeeImportJobAsync(
                file,
                CardType.Plastic,
                CardDesignType.NeedCustomDesign,
                "Design notes");
        }

        [Fact]
        public async Task PlaceBulkOrderFromExcel_ReturnsErrorStatusCode_OnFailure()
        {
            var file = Substitute.For<IFormFile>();
            var request = new ImportEmployeesAndOrderCardsRequest { File = file };

            _cardOrderService.QueueEmployeeImportJobAsync(
                file,
                Arg.Any<CardType>(),
                Arg.Any<CardDesignType>(),
                Arg.Any<string>()).Returns(ServiceResult<EmployeeImportJob>.Fail("Validation failed", 422));

            var result = await _sut.PlaceBulkOrderFromExcel(request) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(422, result.StatusCode);
        }
    }
}
