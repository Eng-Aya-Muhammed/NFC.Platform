namespace NFC.Platform.Tests.Services;

public class CardPackageServiceTests
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IMessageService _messageService;
    private readonly CardPackageService _service;
    private readonly IGenericRepository<CardPackage> _packageRepo;

    public CardPackageServiceTests()
    {
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _messageService = Substitute.For<IMessageService>();
        _packageRepo = Substitute.For<IGenericRepository<CardPackage>>();

        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<CardPackageMappingProfile>();
        });
        _mapper = config.CreateMapper();

        _unitOfWork.Repository<CardPackage>().Returns(_packageRepo);
        _messageService.Get(Arg.Any<string>(), Arg.Any<object[]>()).Returns(x => (string)x[0]);

        _service = new CardPackageService(_unitOfWork, _mapper, _messageService);
    }

    [Fact]
    public async Task GetActiveCardPackagesAsync_ReturnsOnlyActivePackagesOrderedByCardCount()
    {
        // Arrange
        var packages = new List<CardPackage>
        {
            new() { Id = Guid.NewGuid(), NumberOfCards = 10, Price = 100, IsActive = true },
            new() { Id = Guid.NewGuid(), NumberOfCards = 5, Price = 60, IsActive = true },
            new() { Id = Guid.NewGuid(), NumberOfCards = 20, Price = 180, IsActive = false }
        };

        _packageRepo.GetQueryable().Returns(packages.AsQueryable().BuildMock());

        // Act
        var result = await _service.GetActiveCardPackagesAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data!.Count);
        Assert.Equal(5, result.Data[0].NumberOfCards);
        Assert.Equal(10, result.Data[1].NumberOfCards);
    }

    [Fact]
    public async Task GetAllAdminCardPackagesAsync_ReturnsPagedAdminDtos()
    {
        // Arrange
        var packages = new List<CardPackage>
        {
            new() { Id = Guid.NewGuid(), NumberOfCards = 10, Price = 100, IsActive = true }
        };

        _packageRepo.GetQueryable().Returns(packages.AsQueryable().BuildMock());

        var paginationRequest = new PaginationRequest { PageNumber = 1, PageSize = 10 };

        // Act
        var result = await _service.GetAllAdminCardPackagesAsync(paginationRequest);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Data!.TotalCount);
        Assert.Single(result.Data.Items);
        Assert.Equal(10, result.Data.Items[0].NumberOfCards);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsSuccess_WhenPackageExists()
    {
        // Arrange
        var id = Guid.NewGuid();
        var package = new CardPackage { Id = id, NumberOfCards = 10, Price = 100 };
        _packageRepo.GetByIdAsync(id).Returns(package);

        // Act
        var result = await _service.GetByIdAsync(id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(10, result.Data.NumberOfCards);
        Assert.Equal(100, result.Data.Price);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNotFound_WhenPackageDoesNotExist()
    {
        // Arrange
        var id = Guid.NewGuid();
        _packageRepo.GetByIdAsync(id).Returns((CardPackage?)null);

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
        _packageRepo.GetQueryable().Returns(new List<CardPackage>().AsQueryable().BuildMock());
        var request = new CreateCardPackageRequest { NumberOfCards = 15, Price = 120, IsActive = true };

        // Act
        var result = await _service.CreateAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        await _packageRepo.Received(1).AddAsync(Arg.Any<CardPackage>());
        await _unitOfWork.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task CreateAsync_Fails_WhenNumberOfCardsAlreadyExists()
    {
        // Arrange
        var existing = new List<CardPackage> { new() { Id = Guid.NewGuid(), NumberOfCards = 10, Price = 100 } };
        _packageRepo.GetQueryable().Returns(existing.AsQueryable().BuildMock());

        var request = new CreateCardPackageRequest { NumberOfCards = 10, Price = 150 };

        // Act
        var result = await _service.CreateAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal("DuplicatePackageNumberOfCards", result.Message);
        await _packageRepo.DidNotReceive().AddAsync(Arg.Any<CardPackage>());
    }

    [Fact]
    public async Task UpdateAsync_ReturnsSuccess_WhenUpdateIsValid()
    {
        // Arrange
        var id = Guid.NewGuid();
        var existing = new CardPackage { Id = id, NumberOfCards = 10, Price = 100 };
        _packageRepo.GetByIdAsync(id).Returns(existing);
        _packageRepo.GetQueryable().Returns(new List<CardPackage> { existing }.AsQueryable().BuildMock());

        var request = new UpdateCardPackageRequest { Price = 110 };

        // Act
        var result = await _service.UpdateAsync(id, request);

        // Assert
        Assert.True(result.IsSuccess);
        _packageRepo.Received(1).Update(existing);
        await _unitOfWork.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task UpdateAsync_Fails_WhenPackageNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        _packageRepo.GetByIdAsync(id).Returns((CardPackage?)null);

        var request = new UpdateCardPackageRequest { Price = 120 };

        // Act
        var result = await _service.UpdateAsync(id, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task UpdateAsync_Fails_WhenDuplicateNumberOfCardsProvided()
    {
        // Arrange
        var id = Guid.NewGuid();
        var existingPackage = new CardPackage { Id = id, NumberOfCards = 10, Price = 100 };
        var otherPackage = new CardPackage { Id = Guid.NewGuid(), NumberOfCards = 20, Price = 180 };

        _packageRepo.GetByIdAsync(id).Returns(existingPackage);
        _packageRepo.GetQueryable().Returns(new List<CardPackage> { existingPackage, otherPackage }.AsQueryable().BuildMock());

        var request = new UpdateCardPackageRequest { NumberOfCards = 20 };

        // Act
        var result = await _service.UpdateAsync(id, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal("DuplicatePackageNumberOfCards", result.Message);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsSuccess_WhenPackageExists()
    {
        // Arrange
        var id = Guid.NewGuid();
        var existing = new CardPackage { Id = id, NumberOfCards = 10, Price = 100 };
        _packageRepo.GetByIdAsync(id).Returns(existing);

        // Act
        var result = await _service.DeleteAsync(id);

        // Assert
        Assert.True(result.IsSuccess);
        _packageRepo.Received(1).Remove(existing);
        await _unitOfWork.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task DeleteAsync_Fails_WhenPackageNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        _packageRepo.GetByIdAsync(id).Returns((CardPackage?)null);

        // Act
        var result = await _service.DeleteAsync(id);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task GetActiveCardPackagesAsync_FiltersBySearch()
    {
        // Arrange
        var packages = new List<CardPackage>
        {
            new() { Id = Guid.NewGuid(), NumberOfCards = 10, Price = 100, IsActive = true },
            new() { Id = Guid.NewGuid(), NumberOfCards = 50, Price = 450, IsActive = true }
        };

        _packageRepo.GetQueryable().Returns(packages.AsQueryable().BuildMock());

        // Act
        var result = await _service.GetActiveCardPackagesAsync("50");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Data!);
        Assert.Equal(50, result.Data![0].NumberOfCards);
    }

    [Fact]
    public async Task GetAllAdminCardPackagesAsync_FiltersBySearch()
    {
        // Arrange
        var packages = new List<CardPackage>
        {
            new() { Id = Guid.NewGuid(), NumberOfCards = 10, Price = 100, IsActive = true },
            new() { Id = Guid.NewGuid(), NumberOfCards = 50, Price = 450, IsActive = true }
        };

        _packageRepo.GetQueryable().Returns(packages.AsQueryable().BuildMock());
        var paginationRequest = new PaginationRequest { PageNumber = 1, PageSize = 10 };

        // Act
        var result = await _service.GetAllAdminCardPackagesAsync(paginationRequest, "450");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Data!.TotalCount);
        Assert.Equal(50, result.Data.Items[0].NumberOfCards);
    }
}
