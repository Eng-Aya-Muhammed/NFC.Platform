using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;
using NFC.Platform.API.Controllers.Admin;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Application.DTOs.Admin;
using NFC.Platform.Application.DTOs.CardOrder;
using NFC.Platform.Application.DTOs.CardPackage;
using NFC.Platform.Application.DTOs.CardTemplate;
using NFC.Platform.Application.DTOs.CardType;
using NFC.Platform.Application.DTOs.DiscountCode;
using NFC.Platform.Application.DTOs.Subscription;
using NFC.Platform.Application.DTOs.Template;
using NFC.Platform.Application.DTOs.TemplateCategory;
using NFC.Platform.Application.Interfaces.Services;
using NFC.Platform.BuildingBlocks.Common.Constants;
using NFC.Platform.BuildingBlocks.Results;
using NFC.Platform.Domain.Enums;

namespace NFC.Platform.Tests.Controllers;

public class AdminOrdersControllerTests
{
    private readonly IAdminService _adminService = Substitute.For<IAdminService>();
    private readonly AdminOrdersController _sut;

    public AdminOrdersControllerTests()
    {
        _sut = new AdminOrdersController(_adminService);
    }

    [Fact]
    public void AdminOrdersController_ShouldHaveAuthorizeAttributeWithAdminOnlyPolicy()
    {
        var type = typeof(AdminOrdersController);
        var attributes = type.GetCustomAttributes(typeof(AuthorizeAttribute), true);
        Assert.NotEmpty(attributes);
        var auth = attributes.First() as AuthorizeAttribute;
        Assert.NotNull(auth);
        Assert.Equal(AppPolicies.AdminOnly, auth.Policy);
    }

    [Fact]
    public async Task GetOrdersPaged_CallsAdminService_AndReturnsOk()
    {
        var request = new PaginationRequest();
        var status = OrderStatus.InPrinting;
        var companyId = Guid.NewGuid();
        var expectedResult = ServiceResult<PagedResult<AdminOrderSummaryDto>>.Success(
            PagedResult<AdminOrderSummaryDto>.Create(new List<AdminOrderSummaryDto>(), 0, 1, 10));

        _adminService.GetOrdersPagedAsync(request, status, companyId, null, null, Arg.Any<CancellationToken>()).Returns(expectedResult);

        var result = await _sut.GetOrdersPaged(request, status, companyId, null, null, CancellationToken.None) as OkObjectResult;

        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
        await _adminService.Received(1).GetOrdersPagedAsync(request, status, companyId, null, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetOrderById_ReturnsOk_WhenSuccess()
    {
        var id = Guid.NewGuid();
        _adminService.GetOrderByIdAsync(id).Returns(ServiceResult<AdminOrderDetailDto>.Success(new AdminOrderDetailDto()));

        var result = await _sut.GetOrderById(id) as OkObjectResult;

        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
    }

    [Fact]
    public async Task UpdateOrderStatus_ReturnsOk_WhenSuccess()
    {
        var id = Guid.NewGuid();
        var dto = new UpdateOrderStatusDto();
        _adminService.UpdateOrderStatusAsync(id, dto).Returns(ServiceResult<AdminOrderDetailDto>.Success(new AdminOrderDetailDto()));

        var result = await _sut.UpdateOrderStatus(id, dto) as OkObjectResult;

        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
    }

    [Fact]
    public async Task VerifyDeliveryOtp_ReturnsOk_WhenSuccess()
    {
        var id = Guid.NewGuid();
        var req = new VerifyDeliveryOtpRequest { Otp = "123456" };
        _adminService.VerifyDeliveryOtpAsync(id, "123456").Returns(ServiceResult<bool>.Success(true));

        var result = await _sut.VerifyDeliveryOtp(id, req) as OkObjectResult;

        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
    }

    [Fact]
    public async Task ResendDeliveryOtp_ReturnsOk_WhenSuccess()
    {
        var id = Guid.NewGuid();
        _adminService.ResendDeliveryOtpAsync(id).Returns(ServiceResult<bool>.Success(true));

        var result = await _sut.ResendDeliveryOtp(id) as OkObjectResult;

        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
    }
}

public class AdminTemplateRequestsControllerTests
{
    private readonly IAdminService _adminService = Substitute.For<IAdminService>();
    private readonly AdminTemplateRequestsController _sut;

    public AdminTemplateRequestsControllerTests()
    {
        _sut = new AdminTemplateRequestsController(_adminService);
    }

    [Fact]
    public async Task GetTemplateRequestsPaged_ReturnsOk()
    {
        var request = new PaginationRequest();
        _adminService.GetTemplateRequestsPagedAsync(request, null, null, Arg.Any<CancellationToken>()).Returns(ServiceResult<PagedResult<TemplateRequestDto>>.Success(
            PagedResult<TemplateRequestDto>.Create(new List<TemplateRequestDto>(), 0, 1, 10)));

        var result = await _sut.GetTemplateRequestsPaged(request, null, null, CancellationToken.None) as OkObjectResult;

        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
    }

    [Fact]
    public async Task ResolveTemplateRequest_ReturnsOk_WhenSuccess()
    {
        var id = Guid.NewGuid();
        var dto = new ResolveTemplateRequestDto();
        _adminService.ResolveTemplateRequestAsync(id, dto).Returns(ServiceResult<TemplateRequestDto>.Success(new TemplateRequestDto()));

        var result = await _sut.ResolveTemplateRequest(id, dto) as OkObjectResult;

        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
    }
}

public class AdminCardTemplatesControllerTests
{
    private readonly ICardTemplateService _cardTemplateService = Substitute.For<ICardTemplateService>();
    private readonly AdminCardTemplatesController _sut;

    public AdminCardTemplatesControllerTests()
    {
        _sut = new AdminCardTemplatesController(_cardTemplateService);
    }

    [Fact]
    public async Task GetAllAdminCardTemplates_ReturnsOk()
    {
        var req = new PaginationRequest();
        _cardTemplateService.GetAllAdminTemplatesAsync(req).Returns(ServiceResult<PagedResult<CardTemplateAdminDto>>.Success(
            PagedResult<CardTemplateAdminDto>.Create(new List<CardTemplateAdminDto>(), 0, 1, 10)));

        var result = await _sut.GetAllAdminCardTemplates(req) as OkObjectResult;

        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
    }
}

public class AdminDiscountCodesControllerTests
{
    private readonly IDiscountCodeService _discountCodeService = Substitute.For<IDiscountCodeService>();
    private readonly AdminDiscountCodesController _sut;

    public AdminDiscountCodesControllerTests()
    {
        _sut = new AdminDiscountCodesController(_discountCodeService);
    }

    [Fact]
    public async Task GetDiscountCodesPaged_ReturnsOk()
    {
        var req = new PaginationRequest();
        _discountCodeService.GetPagedAdminAsync(req, null, Arg.Any<CancellationToken>()).Returns(ServiceResult<PagedResult<DiscountCodeDto>>.Success(
            PagedResult<DiscountCodeDto>.Create(new List<DiscountCodeDto>(), 0, 1, 10)));

        var result = await _sut.GetDiscountCodesPaged(req, null, CancellationToken.None) as OkObjectResult;

        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
    }
}
