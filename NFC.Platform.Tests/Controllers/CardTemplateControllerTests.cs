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
