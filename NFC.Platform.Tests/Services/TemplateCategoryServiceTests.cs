namespace NFC.Platform.Tests.Services;

public class TemplateCategoryServiceTests
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IMessageService _messageService;
    private readonly TemplateCategoryService _service;
    private readonly IGenericRepository<TemplateCategory> _categoryRepo;

    public TemplateCategoryServiceTests()
    {
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _messageService = Substitute.For<IMessageService>();
        _categoryRepo = Substitute.For<IGenericRepository<TemplateCategory>>();

        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<TemplateCategoryMappingProfile>();
        });
        _mapper = config.CreateMapper();

        _unitOfWork.Repository<TemplateCategory>().Returns(_categoryRepo);
        _messageService.Get(Arg.Any<string>(), Arg.Any<object[]>()).Returns(x => (string)x[0]);

        _service = new TemplateCategoryService(_unitOfWork, _mapper, _messageService);
    }

    [Fact]
    public async Task GetActiveCategoriesAsync_ReturnsActiveCategoriesOrderedByDisplayOrder()
    {
        var categories = new List<TemplateCategory>
        {
            new() { Id = Guid.NewGuid(), NameAr = "فئة 1", NameEn = "Cat 1", DisplayOrder = 2, IsActive = true },
            new() { Id = Guid.NewGuid(), NameAr = "فئة 2", NameEn = "Cat 2", DisplayOrder = 1, IsActive = true },
            new() { Id = Guid.NewGuid(), NameAr = "فئة 3", NameEn = "Cat 3", DisplayOrder = 3, IsActive = false }
        };

        _categoryRepo.GetQueryable().Returns(categories.AsQueryable().BuildMock());

        var result = await _service.GetActiveCategoriesAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data!.Count);
        Assert.Equal("Cat 2", result.Data[0].Name);
        Assert.Equal("Cat 1", result.Data[1].Name);
    }

    [Fact]
    public async Task GetAllAdminCategoriesAsync_ReturnsPagedAdminDtos()
    {
        var categories = new List<TemplateCategory>
        {
            new() { Id = Guid.NewGuid(), NameAr = "فئة 1", NameEn = "Cat 1", DisplayOrder = 1, IsActive = true }
        };

        _categoryRepo.GetQueryable().Returns(categories.AsQueryable().BuildMock());

        var paginationRequest = new PaginationRequest { PageNumber = 1, PageSize = 10 };

        var result = await _service.GetAllAdminCategoriesAsync(paginationRequest);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Data!.TotalCount);
        Assert.Single(result.Data.Items);
        Assert.Equal("فئة 1", result.Data.Items[0].NameAr);
        Assert.Equal("Cat 1", result.Data.Items[0].NameEn);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsSuccess_WhenCategoryExists()
    {
        var id = Guid.NewGuid();
        var category = new TemplateCategory { Id = id, NameAr = "فئة", NameEn = "Category", DisplayOrder = 1 };
        _categoryRepo.GetByIdAsync(id).Returns(category);

        var result = await _service.GetByIdAsync(id);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("فئة", result.Data.NameAr);
        Assert.Equal("Category", result.Data.NameEn);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNotFound_WhenCategoryDoesNotExist()
    {
        var id = Guid.NewGuid();
        _categoryRepo.GetByIdAsync(id).Returns((TemplateCategory?)null);

        var result = await _service.GetByIdAsync(id);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task CreateAsync_ReturnsSuccess_WhenRequestIsValid()
    {
        _categoryRepo.GetQueryable().Returns(new List<TemplateCategory>().AsQueryable().BuildMock());
        var request = new CreateTemplateCategoryRequest { NameAr = "جديد", NameEn = "New", DisplayOrder = 1 };

        var result = await _service.CreateAsync(request);

        Assert.True(result.IsSuccess);
        await _categoryRepo.Received(1).AddAsync(Arg.Any<TemplateCategory>());
        await _unitOfWork.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task CreateAsync_Fails_WhenNameArAlreadyExists()
    {
        var existing = new List<TemplateCategory> { new() { Id = Guid.NewGuid(), NameAr = "موجود", NameEn = "Existing" } };
        _categoryRepo.GetQueryable().Returns(existing.AsQueryable().BuildMock());

        var request = new CreateTemplateCategoryRequest { NameAr = "موجود", NameEn = "New" };

        var result = await _service.CreateAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal("DuplicateNameAr", result.Message);
        await _categoryRepo.DidNotReceive().AddAsync(Arg.Any<TemplateCategory>());
    }

    [Fact]
    public async Task CreateAsync_Fails_WhenNameEnAlreadyExists()
    {
        var existing = new List<TemplateCategory> { new() { Id = Guid.NewGuid(), NameAr = "موجود", NameEn = "Existing" } };
        _categoryRepo.GetQueryable().Returns(existing.AsQueryable().BuildMock());

        var request = new CreateTemplateCategoryRequest { NameAr = "جديد", NameEn = "Existing" };

        var result = await _service.CreateAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal("DuplicateNameEn", result.Message);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsSuccess_WhenUpdateIsValid()
    {
        var id = Guid.NewGuid();
        var existing = new TemplateCategory { Id = id, NameAr = "قديم", NameEn = "Old" };
        _categoryRepo.GetByIdAsync(id).Returns(existing);
        _categoryRepo.GetQueryable().Returns(new List<TemplateCategory> { existing }.AsQueryable().BuildMock());

        var request = new UpdateTemplateCategoryRequest { NameAr = "محدث", NameEn = "Updated" };

        var result = await _service.UpdateAsync(id, request);

        Assert.True(result.IsSuccess);
        _categoryRepo.Received(1).Update(existing);
        await _unitOfWork.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task UpdateAsync_Fails_WhenCategoryNotFound()
    {
        var id = Guid.NewGuid();
        _categoryRepo.GetByIdAsync(id).Returns((TemplateCategory?)null);

        var request = new UpdateTemplateCategoryRequest { NameAr = "محدث" };

        var result = await _service.UpdateAsync(id, request);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task UpdateAsync_Fails_WhenDuplicateNameArProvided()
    {
        var id = Guid.NewGuid();
        var existingCategory = new TemplateCategory { Id = id, NameAr = "فئة 1", NameEn = "Cat 1" };
        var otherCategory = new TemplateCategory { Id = Guid.NewGuid(), NameAr = "فئة 2", NameEn = "Cat 2" };

        _categoryRepo.GetByIdAsync(id).Returns(existingCategory);
        _categoryRepo.GetQueryable().Returns(new List<TemplateCategory> { existingCategory, otherCategory }.AsQueryable().BuildMock());

        var request = new UpdateTemplateCategoryRequest { NameAr = "فئة 2" };

        var result = await _service.UpdateAsync(id, request);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal("DuplicateNameAr", result.Message);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsSuccess_WhenCategoryExists()
    {
        var id = Guid.NewGuid();
        var existing = new TemplateCategory { Id = id, NameAr = "فئة", NameEn = "Cat" };
        _categoryRepo.GetByIdAsync(id).Returns(existing);

        var result = await _service.DeleteAsync(id);

        Assert.True(result.IsSuccess);
        _categoryRepo.Received(1).Remove(existing);
        await _unitOfWork.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task DeleteAsync_Fails_WhenCategoryNotFound()
    {
        var id = Guid.NewGuid();
        _categoryRepo.GetByIdAsync(id).Returns((TemplateCategory?)null);

        var result = await _service.DeleteAsync(id);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
    }
}
