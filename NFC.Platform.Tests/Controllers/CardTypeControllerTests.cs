namespace NFC.Platform.Tests.Controllers;

public class CardTypeControllerTests
{
    private readonly ICardTypeService _cardTypeService;
    private readonly CardTypeController _sut;

    public CardTypeControllerTests()
    {
        _cardTypeService = Substitute.For<ICardTypeService>();
        _sut = new CardTypeController(_cardTypeService);
    }

    [Fact]
    public async Task GetActiveCardTypes_ReturnsOkResult_WithActiveCardTypes()
    {
        // Arrange
        var cardTypes = new List<CardTypeDto>
        {
            new() { Id = Guid.NewGuid(), Name = "Plastic" },
            new() { Id = Guid.NewGuid(), Name = "Metal" }
        };

        var serviceResult = ServiceResult<IReadOnlyList<CardTypeDto>>.Success(cardTypes);
        _cardTypeService.GetActiveCardTypesAsync("Metal").Returns(serviceResult);

        // Act
        var result = await _sut.GetActiveCardTypes("Metal") as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
        var returnResult = Assert.IsType<ServiceResult<IReadOnlyList<CardTypeDto>>>(result.Value);
        Assert.True(returnResult.IsSuccess);
        Assert.Equal(2, returnResult.Data!.Count);
        await _cardTypeService.Received(1).GetActiveCardTypesAsync("Metal");
    }
}
