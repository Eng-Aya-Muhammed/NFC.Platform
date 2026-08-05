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
        // Arrange
        var cardTypes = new List<CardType>
        {
            new() { Id = Guid.NewGuid(), NameAr = "نوع 1", NameEn = "Type 1", IsActive = true },
            new() { Id = Guid.NewGuid(), NameAr = "نوع 2", NameEn = "Type 2", IsActive = false }
        };

        _cardTypeRepo.GetQueryable().Returns(cardTypes.AsQueryable().BuildMock());

        // Act
        var result = await _service.GetActiveCardTypesAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Data!);
        Assert.Equal("Type 1", result.Data![0].Name);
    }

    [Fact]
    public async Task GetAllAdminCardTypesAsync_ReturnsPagedAdminDtos()
    {
        // Arrange
        var cardTypes = new List<CardType>
        {
            new() { Id = Guid.NewGuid(), NameAr = "نوع 1", NameEn = "Type 1", IsActive = true }
        };

        _cardTypeRepo.GetQueryable().Returns(cardTypes.AsQueryable().BuildMock());

        var paginationRequest = new PaginationRequest { PageNumber = 1, PageSize = 10 };

        // Act
        var result = await _service.GetAllAdminCardTypesAsync(paginationRequest);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Data!.TotalCount);
        Assert.Single(result.Data.Items);
        Assert.Equal("نوع 1", result.Data.Items[0].NameAr);
        Assert.Equal("Type 1", result.Data.Items[0].NameEn);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsSuccess_WhenCardTypeExists()
    {
        // Arrange
        var id = Guid.NewGuid();
        var cardType = new CardType { Id = id, NameAr = "بلاستيك", NameEn = "Plastic" };
        _cardTypeRepo.GetByIdAsync(id).Returns(cardType);

        // Act
        var result = await _service.GetByIdAsync(id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("بلاستيك", result.Data.NameAr);
        Assert.Equal("Plastic", result.Data.NameEn);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNotFound_WhenCardTypeDoesNotExist()
    {
        // Arrange
        var id = Guid.NewGuid();
        _cardTypeRepo.GetByIdAsync(id).Returns((CardType?)null);

        // Act
        var result = await _service.GetByIdAsync(id);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task CreateAsync_ReturnsSuccess_WhenRequestIsValid()
    {
        // Arrange
        _cardTypeRepo.GetQueryable().Returns(new List<CardType>().AsQueryable().BuildMock());
        var request = new CreateCardTypeRequest { NameAr = "معدني", NameEn = "Metal" };

        // Act
        var result = await _service.CreateAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        await _cardTypeRepo.Received(1).AddAsync(Arg.Any<CardType>());
        await _unitOfWork.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task CreateAsync_Fails_WhenNameArAlreadyExists()
    {
        // Arrange
        var existing = new List<CardType> { new() { Id = Guid.NewGuid(), NameAr = "موجود", NameEn = "Existing" } };
        _cardTypeRepo.GetQueryable().Returns(existing.AsQueryable().BuildMock());

        var request = new CreateCardTypeRequest { NameAr = "موجود", NameEn = "New" };

        // Act
        var result = await _service.CreateAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal("DuplicateNameAr", result.Message);
    }

    [Fact]
    public async Task CreateAsync_Fails_WhenNameEnAlreadyExists()
    {
        // Arrange
        var existing = new List<CardType> { new() { Id = Guid.NewGuid(), NameAr = "موجود", NameEn = "Existing" } };
        _cardTypeRepo.GetQueryable().Returns(existing.AsQueryable().BuildMock());

        var request = new CreateCardTypeRequest { NameAr = "جديد", NameEn = "Existing" };

        // Act
        var result = await _service.CreateAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal("DuplicateNameEn", result.Message);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsSuccess_WhenUpdateIsValid()
    {
        // Arrange
        var id = Guid.NewGuid();
        var existing = new CardType { Id = id, NameAr = "قديم", NameEn = "Old" };
        _cardTypeRepo.GetByIdAsync(id).Returns(existing);
        _cardTypeRepo.GetQueryable().Returns(new List<CardType> { existing }.AsQueryable().BuildMock());

        var request = new UpdateCardTypeRequest { NameAr = "محدث", NameEn = "Updated" };

        // Act
        var result = await _service.UpdateAsync(id, request);

        // Assert
        Assert.True(result.IsSuccess);
        _cardTypeRepo.Received(1).Update(existing);
        await _unitOfWork.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task UpdateAsync_Fails_WhenCardTypeNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        _cardTypeRepo.GetByIdAsync(id).Returns((CardType?)null);

        var request = new UpdateCardTypeRequest { NameAr = "محدث" };

        // Act
        var result = await _service.UpdateAsync(id, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsSuccess_WhenCardTypeExists()
    {
        // Arrange
        var id = Guid.NewGuid();
        var existing = new CardType { Id = id, NameAr = "نوع", NameEn = "Type" };
        _cardTypeRepo.GetByIdAsync(id).Returns(existing);

        // Act
        var result = await _service.DeleteAsync(id);

        // Assert
        Assert.True(result.IsSuccess);
        _cardTypeRepo.Received(1).Remove(existing);
        await _unitOfWork.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task DeleteAsync_Fails_WhenCardTypeNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        _cardTypeRepo.GetByIdAsync(id).Returns((CardType?)null);

        // Act
        var result = await _service.DeleteAsync(id);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task GetActiveCardTypesAsync_FiltersBySearch()
    {
        // Arrange
        var cardTypes = new List<CardType>
        {
            new() { Id = Guid.NewGuid(), NameAr = "خشب", NameEn = "Wood", IsActive = true },
            new() { Id = Guid.NewGuid(), NameAr = "معدن", NameEn = "Metal", IsActive = true }
        };

        _cardTypeRepo.GetQueryable().Returns(cardTypes.AsQueryable().BuildMock());

        // Act
        var result = await _service.GetActiveCardTypesAsync("Metal");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Data!);
        Assert.Equal("Metal", result.Data![0].Name);
    }

    [Fact]
    public async Task GetAllAdminCardTypesAsync_FiltersBySearch()
    {
        // Arrange
        var cardTypes = new List<CardType>
        {
            new() { Id = Guid.NewGuid(), NameAr = "خشب", NameEn = "Wood", IsActive = true },
            new() { Id = Guid.NewGuid(), NameAr = "بلاستيك", NameEn = "Plastic", IsActive = true }
        };

        _cardTypeRepo.GetQueryable().Returns(cardTypes.AsQueryable().BuildMock());
        var request = new PaginationRequest { PageNumber = 1, PageSize = 10 };

        // Act
        var result = await _service.GetAllAdminCardTypesAsync(request, "خشب");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Data!.TotalCount);
        Assert.Equal("خشب", result.Data.Items[0].NameAr);
    }
}
