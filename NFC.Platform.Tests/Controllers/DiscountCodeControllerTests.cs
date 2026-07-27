using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;
using NFC.Platform.API.Controllers;
using NFC.Platform.Application.DTOs.DiscountCode;
using NFC.Platform.Application.Interfaces.Services;
using NFC.Platform.BuildingBlocks.Results;

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
        // Arrange
        var request = new ValidateDiscountCodeRequest { Code = "SUMMER20", OrderAmount = 100 };
        var validationResult = new DiscountCodeValidationResultDto { IsValid = true, Code = "SUMMER20", FinalAmount = 80 };
        _discountCodeService.ValidateCodeAsync(request).Returns(ServiceResult<DiscountCodeValidationResultDto>.Success(validationResult));

        // Act
        var result = await _sut.ValidateDiscountCode(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
    }
}
