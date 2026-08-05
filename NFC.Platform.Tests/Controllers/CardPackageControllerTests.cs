namespace NFC.Platform.Tests.Controllers;

public class CardPackageControllerTests
{
    private readonly ICardPackageService _cardPackageService;
    private readonly CardPackageController _sut;

    public CardPackageControllerTests()
    {
        _cardPackageService = Substitute.For<ICardPackageService>();
        _sut = new CardPackageController(_cardPackageService);
    }

    [Fact]
    public async Task GetActiveCardPackages_ReturnsOkResult_WithActiveCardPackages()
    {
        var packages = new List<CardPackageDto>
        {
            new() { Id = Guid.NewGuid(), NumberOfCards = 10, Price = 100 },
            new() { Id = Guid.NewGuid(), NumberOfCards = 20, Price = 180 }
        };

        var serviceResult = ServiceResult<IReadOnlyList<CardPackageDto>>.Success(packages);
        _cardPackageService.GetActiveCardPackagesAsync("10").Returns(serviceResult);

        var result = await _sut.GetActiveCardPackages("10") as OkObjectResult;

        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
        var returnResult = Assert.IsType<ServiceResult<IReadOnlyList<CardPackageDto>>>(result.Value);
        Assert.True(returnResult.IsSuccess);
        Assert.Equal(2, returnResult.Data!.Count);
        await _cardPackageService.Received(1).GetActiveCardPackagesAsync("10");
    }
}
