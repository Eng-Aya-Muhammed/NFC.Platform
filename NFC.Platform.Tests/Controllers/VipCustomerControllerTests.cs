using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NFC.Platform.API.Controllers;
using NFC.Platform.API.Controllers.Admin;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Application.DTOs.VipCustomer;
using NFC.Platform.Application.Interfaces.Services;
using NFC.Platform.BuildingBlocks.Results;
using NFC.Platform.Domain.Enums;
using Xunit;

namespace NFC.Platform.Tests.Controllers;

public class VipCustomerControllerTests
{
    private readonly IVipCustomerService _vipCustomerService;
    private readonly ICompanyService _companyService;
    private readonly IProfileService _profileService;
    private readonly VipCustomerController _publicController;
    private readonly AdminVipCustomersController _adminController;

    public VipCustomerControllerTests()
    {
        _vipCustomerService = Substitute.For<IVipCustomerService>();
        _companyService = Substitute.For<ICompanyService>();
        _profileService = Substitute.For<IProfileService>();

        _publicController = new VipCustomerController(_vipCustomerService);
        _adminController = new AdminVipCustomersController(_vipCustomerService, _companyService, _profileService);
    }

    [Fact]
    public async Task GetPublicVipCustomers_ReturnsOkWithData()
    {
        // Arrange
        var list = new List<VipCustomerDto>
        {
            new VipCustomerDto { Id = Guid.NewGuid(), Name = "Spotify", CustomerType = VipCustomerType.Company, IsVip = true }
        };
        _vipCustomerService.GetPublicVipCustomersAsync().Returns(ServiceResult<IReadOnlyList<VipCustomerDto>>.Success(list));

        // Act
        var actionResult = await _publicController.GetPublicVipCustomers();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var result = Assert.IsType<ServiceResult<IReadOnlyList<VipCustomerDto>>>(okResult.Value);
        Assert.True(result.IsSuccess);
        Assert.Single(result.Data);
    }

    [Fact]
    public async Task UpdateCompanyVipStatus_ReturnsOkWithData()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var request = new UpdateVipStatusRequest { IsVip = true, VipDisplayOrder = 1 };
        var dto = new VipCustomerDto { Id = companyId, Name = "Spotify", IsVip = true, VipDisplayOrder = 1, CustomerType = VipCustomerType.Company };

        _companyService.UpdateVipStatusAsync(companyId, request).Returns(ServiceResult<VipCustomerDto>.Success(dto));

        // Act
        var actionResult = await _adminController.UpdateCompanyVipStatus(companyId, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var result = Assert.IsType<ServiceResult<VipCustomerDto>>(okResult.Value);
        Assert.True(result.IsSuccess);
        Assert.True(result.Data.IsVip);
    }
}
