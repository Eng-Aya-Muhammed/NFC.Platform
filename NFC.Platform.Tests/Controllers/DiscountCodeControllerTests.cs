using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NFC.Platform.API.Controllers;
using NFC.Platform.Application.DTOs.DiscountCode;
using NFC.Platform.Application.Interfaces.Services;
using NFC.Platform.BuildingBlocks.Results;
using NSubstitute;
using Xunit;

namespace NFC.Platform.Tests.Controllers;

public class DiscountCodeControllerTests
{
    private readonly IDiscountCodeService _discountCodeService;
    private readonly DiscountCodeController _sut;

    public DiscountCodeControllerTests()
    {
        _discountCodeService = Substitute.For<IDiscountCodeService>();
        _sut = new DiscountCodeController(_discountCodeService);
    }

    [Fact]
    public async Task ValidateDiscountCode_ReturnsOkResult_OnSuccess()
    {
        var request = new ValidateDiscountCodeRequest { Code = "SUMMER20", OrderAmount = 100 };
        var validationResult = new DiscountCodeValidationResultDto { IsValid = true, Code = "SUMMER20", FinalAmount = 80 };
        _discountCodeService.ValidateCodeAsync(request).Returns(ServiceResult<DiscountCodeValidationResultDto>.Success(validationResult));

        var result = await _sut.ValidateDiscountCode(request) as OkObjectResult;

        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
    }

    [Fact]
    public async Task ValidateDiscountCode_ReturnsError_WhenInvalid()
    {
        var request = new ValidateDiscountCodeRequest { Code = "INVALID", OrderAmount = 100 };
        _discountCodeService.ValidateCodeAsync(request).Returns(ServiceResult<DiscountCodeValidationResultDto>.Fail("كود الخصم غير صالح", 400));

        var result = await _sut.ValidateDiscountCode(request) as ObjectResult;

        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
    }
}
