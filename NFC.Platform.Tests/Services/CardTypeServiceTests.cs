namespace NFC.Platform.Tests.Services;

public class CardTypeServiceTests
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IMessageService _messageService;
    private readonly CardTypeService _service;
    private readonly IGenericRepository<CardType> _cardTypeRepo;

    public CardTypeServiceTests()
    {
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _messageService = Substitute.For<IMessageService>();
        _cardTypeRepo = Substitute.For<IGenericRepository<CardType>>();

        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<CardTypeMappingProfile>();
        });
        _mapper = config.CreateMapper();

        _unitOfWork.Repository<CardType>().Returns(_cardTypeRepo);
        _messageService.Get(Arg.Any<string>(), Arg.Any<object[]>()).Returns(x => (string)x[0]);

        _service = new CardTypeService(_unitOfWork, _mapper, _messageService);
    }

    [Fact]
    public async Task GetActiveCardTypesAsync_ReturnsOnlyActiveCardTypes()
    {
        var cardTypes = new List<CardType>
        {
            new() { Id = Guid.NewGuid(), NameAr = "نوع 1", NameEn = "Type 1", IsActive = true },
            new() { Id = Guid.NewGuid(), NameAr = "نوع 2", NameEn = "Type 2", IsActive = false }
        };

        _cardTypeRepo.GetQueryable().Returns(cardTypes.AsQueryable().BuildMock());

        var result = await _service.GetActiveCardTypesAsync();

        Assert.True(result.IsSuccess);
        Assert.Single(result.Data!);
        Assert.Equal("Type 1", result.Data![0].Name);
    }

    [Fact]
    public async Task GetAllAdminCardTypesAsync_ReturnsPagedAdminDtos()
    {
        var cardTypes = new List<CardType>
        {
            new() { Id = Guid.NewGuid(), NameAr = "نوع 1", NameEn = "Type 1", IsActive = true }
        };

        _cardTypeRepo.GetQueryable().Returns(cardTypes.AsQueryable().BuildMock());

        var paginationRequest = new PaginationRequest { PageNumber = 1, PageSize = 10 };

        var result = await _service.GetAllAdminCardTypesAsync(paginationRequest);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Data!.TotalCount);
        Assert.Single(result.Data.Items);
        Assert.Equal("نوع 1", result.Data.Items[0].NameAr);
        Assert.Equal("Type 1", result.Data.Items[0].NameEn);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsSuccess_WhenCardTypeExists()
    {
        var id = Guid.NewGuid();
        var cardType = new CardType { Id = id, NameAr = "بلاستيك", NameEn = "Plastic" };
        _cardTypeRepo.GetByIdAsync(id).Returns(cardType);

        var result = await _service.GetByIdAsync(id);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("بلاستيك", result.Data.NameAr);
        Assert.Equal("Plastic", result.Data.NameEn);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNotFound_WhenCardTypeDoesNotExist()
    {
        var id = Guid.NewGuid();
        _cardTypeRepo.GetByIdAsync(id).Returns((CardType?)null);

        var result = await _service.GetByIdAsync(id);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task CreateAsync_ReturnsSuccess_WhenRequestIsValid()
    {
        _cardTypeRepo.GetQueryable().Returns(new List<CardType>().AsQueryable().BuildMock());
        var request = new CreateCardTypeRequest { NameAr = "معدني", NameEn = "Metal" };

        var result = await _service.CreateAsync(request);

        Assert.True(result.IsSuccess);
        await _cardTypeRepo.Received(1).AddAsync(Arg.Any<CardType>());
        await _unitOfWork.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task CreateAsync_Fails_WhenNameArAlreadyExists()
    {
        var existing = new List<CardType> { new() { Id = Guid.NewGuid(), NameAr = "موجود", NameEn = "Existing" } };
        _cardTypeRepo.GetQueryable().Returns(existing.AsQueryable().BuildMock());

        var request = new CreateCardTypeRequest { NameAr = "موجود", NameEn = "New" };

        var result = await _service.CreateAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal("DuplicateNameAr", result.Message);
    }

    [Fact]
    public async Task CreateAsync_Fails_WhenNameEnAlreadyExists()
    {
        var existing = new List<CardType> { new() { Id = Guid.NewGuid(), NameAr = "موجود", NameEn = "Existing" } };
        _cardTypeRepo.GetQueryable().Returns(existing.AsQueryable().BuildMock());

        var request = new CreateCardTypeRequest { NameAr = "جديد", NameEn = "Existing" };

        var result = await _service.CreateAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal("DuplicateNameEn", result.Message);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsSuccess_WhenUpdateIsValid()
    {
        var id = Guid.NewGuid();
        var existing = new CardType { Id = id, NameAr = "قديم", NameEn = "Old" };
        _cardTypeRepo.GetByIdAsync(id).Returns(existing);
        _cardTypeRepo.GetQueryable().Returns(new List<CardType> { existing }.AsQueryable().BuildMock());

        var request = new UpdateCardTypeRequest { NameAr = "محدث", NameEn = "Updated" };

        var result = await _service.UpdateAsync(id, request);

        Assert.True(result.IsSuccess);
        _cardTypeRepo.Received(1).Update(existing);
        await _unitOfWork.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task UpdateAsync_Fails_WhenCardTypeNotFound()
    {
        var id = Guid.NewGuid();
        _cardTypeRepo.GetByIdAsync(id).Returns((CardType?)null);

        var request = new UpdateCardTypeRequest { NameAr = "محدث" };

        var result = await _service.UpdateAsync(id, request);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsSuccess_WhenCardTypeExists()
    {
        var id = Guid.NewGuid();
        var existing = new CardType { Id = id, NameAr = "نوع", NameEn = "Type" };
        _cardTypeRepo.GetByIdAsync(id).Returns(existing);

        var result = await _service.DeleteAsync(id);

        Assert.True(result.IsSuccess);
        _cardTypeRepo.Received(1).Remove(existing);
        await _unitOfWork.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task DeleteAsync_Fails_WhenCardTypeNotFound()
    {
        var id = Guid.NewGuid();
        _cardTypeRepo.GetByIdAsync(id).Returns((CardType?)null);

        var result = await _service.DeleteAsync(id);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task GetActiveCardTypesAsync_FiltersBySearch()
    {
        var cardTypes = new List<CardType>
        {
            new() { Id = Guid.NewGuid(), NameAr = "خشب", NameEn = "Wood", IsActive = true },
            new() { Id = Guid.NewGuid(), NameAr = "معدن", NameEn = "Metal", IsActive = true }
        };

        _cardTypeRepo.GetQueryable().Returns(cardTypes.AsQueryable().BuildMock());

        var result = await _service.GetActiveCardTypesAsync("Metal");

        Assert.True(result.IsSuccess);
        Assert.Single(result.Data!);
        Assert.Equal("Metal", result.Data![0].Name);
    }

    [Fact]
    public async Task GetAllAdminCardTypesAsync_FiltersBySearch()
    {
        var cardTypes = new List<CardType>
        {
            new() { Id = Guid.NewGuid(), NameAr = "خشب", NameEn = "Wood", IsActive = true },
            new() { Id = Guid.NewGuid(), NameAr = "بلاستيك", NameEn = "Plastic", IsActive = true }
        };

        _cardTypeRepo.GetQueryable().Returns(cardTypes.AsQueryable().BuildMock());
        var request = new PaginationRequest { PageNumber = 1, PageSize = 10 };

        var result = await _service.GetAllAdminCardTypesAsync(request, "خشب");

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Data!.TotalCount);
        Assert.Equal("خشب", result.Data.Items[0].NameAr);
    }
}
