namespace NFC.Platform.Tests.Services;

public class CardTemplateServiceTests
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IMessageService _messageService;

    private readonly IGenericRepository<CardTemplate> _templateRepo;
    private readonly IGenericRepository<TemplateCategory> _categoryRepo;

    private readonly CardTemplateService _sut;

    public CardTemplateServiceTests()
    {
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _messageService = Substitute.For<IMessageService>();

        _templateRepo = Substitute.For<IGenericRepository<CardTemplate>>();
        _categoryRepo = Substitute.For<IGenericRepository<TemplateCategory>>();

        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<CardTemplateMappingProfile>();
        });
        _mapper = config.CreateMapper();

        _unitOfWork.Repository<CardTemplate>().Returns(_templateRepo);
        _unitOfWork.Repository<TemplateCategory>().Returns(_categoryRepo);
        _messageService.Get(Arg.Any<string>(), Arg.Any<object[]>()).Returns(x => (string)x[0]);

        _sut = new CardTemplateService(_unitOfWork, _mapper, _messageService);
    }

    [Fact]
    public async Task GetActiveTemplatesAsync_ReturnsOnlyActiveTemplates_OrderedByDisplayOrder()
    {
        // Arrange
        var templates = new List<CardTemplate>
        {
            new() { Id = Guid.NewGuid(), NameAr = "الثاني", NameEn = "Second", IsActive = true, DisplayOrder = 2 },
            new() { Id = Guid.NewGuid(), NameAr = "الأول", NameEn = "First", IsActive = true, DisplayOrder = 1 },
            new() { Id = Guid.NewGuid(), NameAr = "مخفي", NameEn = "Hidden", IsActive = false, DisplayOrder = 0 }
        };
        _templateRepo.GetQueryable().Returns(templates.AsQueryable().BuildMock());

        // Act
        var result = await _sut.GetActiveTemplatesAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data!.Count);
        Assert.Equal("First", result.Data[0].Name);
        Assert.Equal("Second", result.Data[1].Name);
    }

    [Fact]
    public async Task GetAllAdminTemplatesAsync_ReturnsPagedAdminDtos()
    {
        // Arrange
        var templates = new List<CardTemplate>
        {
            new() { Id = Guid.NewGuid(), NameAr = "قالب 1", NameEn = "Template 1", DisplayOrder = 1, IsActive = true }
        };
        _templateRepo.GetQueryable().Returns(templates.AsQueryable().BuildMock());

        var paginationRequest = new PaginationRequest { PageNumber = 1, PageSize = 10 };

        // Act
        var result = await _sut.GetAllAdminTemplatesAsync(paginationRequest);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Data!.TotalCount);
        Assert.Single(result.Data.Items);
        Assert.Equal("قالب 1", result.Data.Items[0].NameAr);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsSuccess_WhenTemplateExists()
    {
        // Arrange
        var id = Guid.NewGuid();
        var template = new CardTemplate { Id = id, NameAr = "قالب", NameEn = "Template", DisplayOrder = 1 };
        _templateRepo.GetQueryable().Returns(new List<CardTemplate> { template }.AsQueryable().BuildMock());

        // Act
        var result = await _sut.GetByIdAsync(id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("قالب", result.Data.NameAr);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNotFound_WhenTemplateDoesNotExist()
    {
        // Arrange
        var id = Guid.NewGuid();
        _templateRepo.GetQueryable().Returns(new List<CardTemplate>().AsQueryable().BuildMock());

        // Act
        var result = await _sut.GetByIdAsync(id);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task CreateAsync_ReturnsSuccess_WhenRequestIsValid()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        _categoryRepo.GetByIdAsync(categoryId).Returns(new TemplateCategory { Id = categoryId });
        _templateRepo.GetQueryable().Returns(new List<CardTemplate>().AsQueryable().BuildMock());

        var request = new CreateCardTemplateRequest
        {
            CategoryId = categoryId,
            NameAr = "قالب جديد",
            NameEn = "New Template",
            DisplayOrder = 1
        };

        // Act
        var result = await _sut.CreateAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        await _templateRepo.Received(1).AddAsync(Arg.Any<CardTemplate>());
        await _unitOfWork.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task CreateAsync_Fails_WhenCategoryDoesNotExist()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        _categoryRepo.GetByIdAsync(categoryId).Returns((TemplateCategory?)null);

        var request = new CreateCardTemplateRequest { CategoryId = categoryId, NameAr = "قالب", NameEn = "Template" };

        // Act
        var result = await _sut.CreateAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal("RecordNotFound", result.Message);
    }

    [Fact]
    public async Task CreateAsync_Fails_WhenNameArAlreadyExists()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        _categoryRepo.GetByIdAsync(categoryId).Returns(new TemplateCategory { Id = categoryId });

        var existing = new List<CardTemplate> { new() { Id = Guid.NewGuid(), NameAr = "موجود", NameEn = "Existing" } };
        _templateRepo.GetQueryable().Returns(existing.AsQueryable().BuildMock());

        var request = new CreateCardTemplateRequest { CategoryId = categoryId, NameAr = "موجود", NameEn = "New" };

        // Act
        var result = await _sut.CreateAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal("DuplicateNameAr", result.Message);
    }

    [Fact]
    public async Task CreateAsync_Fails_WhenNameEnAlreadyExists()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        _categoryRepo.GetByIdAsync(categoryId).Returns(new TemplateCategory { Id = categoryId });

        var existing = new List<CardTemplate> { new() { Id = Guid.NewGuid(), NameAr = "موجود", NameEn = "Existing" } };
        _templateRepo.GetQueryable().Returns(existing.AsQueryable().BuildMock());

        var request = new CreateCardTemplateRequest { CategoryId = categoryId, NameAr = "جديد", NameEn = "Existing" };

        // Act
        var result = await _sut.CreateAsync(request);

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
        var categoryId = Guid.NewGuid();
        var existing = new CardTemplate { Id = id, NameAr = "قديم", NameEn = "Old", CategoryId = categoryId };

        _templateRepo.GetByIdAsync(id).Returns(existing);
        _categoryRepo.GetByIdAsync(categoryId).Returns(new TemplateCategory { Id = categoryId });
        _templateRepo.GetQueryable().Returns(new List<CardTemplate> { existing }.AsQueryable().BuildMock());

        var request = new UpdateCardTemplateRequest { CategoryId = categoryId, NameAr = "محدث", NameEn = "Updated" };

        // Act
        var result = await _sut.UpdateAsync(id, request);

        // Assert
        Assert.True(result.IsSuccess);
        _templateRepo.Received(1).Update(existing);
        await _unitOfWork.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task UpdateAsync_Fails_WhenTemplateNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        _templateRepo.GetByIdAsync(id).Returns((CardTemplate?)null);

        var request = new UpdateCardTemplateRequest { NameAr = "محدث" };

        // Act
        var result = await _sut.UpdateAsync(id, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsSuccess_WhenTemplateExists()
    {
        // Arrange
        var id = Guid.NewGuid();
        var existing = new CardTemplate { Id = id, NameAr = "قالب", NameEn = "Template" };
        _templateRepo.GetByIdAsync(id).Returns(existing);

        // Act
        var result = await _sut.DeleteAsync(id);

        // Assert
        Assert.True(result.IsSuccess);
        _templateRepo.Received(1).Remove(existing);
        await _unitOfWork.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task DeleteAsync_Fails_WhenTemplateNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        _templateRepo.GetByIdAsync(id).Returns((CardTemplate?)null);

        // Act
        var result = await _sut.DeleteAsync(id);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task GetActiveTemplatesAsync_FiltersBySearch()
    {
        // Arrange
        var templates = new List<CardTemplate>
        {
            new() { Id = Guid.NewGuid(), NameAr = "قالب الأعمال", NameEn = "Business Template", IsActive = true, DisplayOrder = 1 },
            new() { Id = Guid.NewGuid(), NameAr = "قالب الإبداع", NameEn = "Creative Template", IsActive = true, DisplayOrder = 2 }
        };
        _templateRepo.GetQueryable().Returns(templates.AsQueryable().BuildMock());

        // Act
        var result = await _sut.GetActiveTemplatesAsync("Business");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Data!);
        Assert.Equal("Business Template", result.Data[0].Name);
    }
}
