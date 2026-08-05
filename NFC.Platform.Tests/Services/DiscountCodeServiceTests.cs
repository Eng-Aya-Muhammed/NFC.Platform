using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using MockQueryable.NSubstitute;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Application.DTOs.DiscountCode;
using NFC.Platform.Application.Interfaces.Repositories;
using NFC.Platform.Application.Services;
using NFC.Platform.BuildingBlocks.Localization;
using NFC.Platform.Domain.Entities;
using NSubstitute;
using Xunit;

namespace NFC.Platform.Tests.Services;

public class DiscountCodeServiceTests
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IMessageService _messageService;
    private readonly IGenericRepository<DiscountCode> _discountCodeRepo;
    private readonly DiscountCodeService _sut;

    public DiscountCodeServiceTests()
    {
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _mapper = Substitute.For<IMapper>();
        _messageService = Substitute.For<IMessageService>();
        _discountCodeRepo = Substitute.For<IGenericRepository<DiscountCode>>();

        _unitOfWork.Repository<DiscountCode>().Returns(_discountCodeRepo);

        _sut = new DiscountCodeService(_unitOfWork, _mapper, _messageService);
    }

    [Fact]
    public async Task GetPagedAdminAsync_ReturnsPagedDiscountCodes()
    {
        var request = new PaginationRequest { PageNumber = 1, PageSize = 10 };
        var codes = new List<DiscountCode>
        {
            new() { Id = Guid.NewGuid(), Code = "SUMMER20", DiscountValue = 20, StartDate = DateTime.UtcNow.AddDays(-1), EndDate = DateTime.UtcNow.AddDays(30) }
        };

        var mockQuery = codes.AsQueryable().BuildMock();
        _discountCodeRepo.GetQueryable().Returns(mockQuery);
        _mapper.Map<DiscountCodeDto>(Arg.Any<DiscountCode>()).Returns(new DiscountCodeDto { Code = "SUMMER20" });

        var result = await _sut.GetPagedAdminAsync(request);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Data!.Items);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNotFound_WhenDoesNotExist()
    {
        var id = Guid.NewGuid();
        var emptyQuery = new List<DiscountCode>().AsQueryable().BuildMock();
        _discountCodeRepo.GetQueryable().Returns(emptyQuery);
        _messageService.Get("RecordNotFound").Returns("Record not found");

        var result = await _sut.GetByIdAsync(id);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task CreateAsync_ReturnsBadRequest_WhenCodeAlreadyExists()
    {
        var request = new CreateDiscountCodeRequest { Code = "summer20", DiscountValue = 20, StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(10) };
        var existing = new List<DiscountCode>
        {
            new() { Id = Guid.NewGuid(), Code = "SUMMER20" }
        };

        var mockQuery = existing.AsQueryable().BuildMock();
        _discountCodeRepo.GetQueryable().Returns(mockQuery);
        _messageService.Get("DuplicateDiscountCode").Returns("Discount code already exists.");

        var result = await _sut.CreateAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task CreateAsync_SavesAndReturnsDto_WhenValid()
    {
        var request = new CreateDiscountCodeRequest { Code = "SUMMER20", DiscountValue = 20, StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(10) };
        var emptyQuery = new List<DiscountCode>().AsQueryable().BuildMock();
        _discountCodeRepo.GetQueryable().Returns(emptyQuery);

        var entity = new DiscountCode { Id = Guid.NewGuid(), Code = "SUMMER20", DiscountValue = 20 };
        _mapper.Map<DiscountCode>(request).Returns(entity);
        _mapper.Map<DiscountCodeDto>(entity).Returns(new DiscountCodeDto { Id = entity.Id, Code = "SUMMER20" });

        var result = await _sut.CreateAsync(request);

        Assert.True(result.IsSuccess);
        await _discountCodeRepo.Received(1).AddAsync(Arg.Any<DiscountCode>());
        await _unitOfWork.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task ValidateCodeAsync_ReturnsInvalid_WhenNotFound()
    {
        var request = new ValidateDiscountCodeRequest { Code = "NONEXISTENT", OrderAmount = 100 };
        var emptyQuery = new List<DiscountCode>().AsQueryable().BuildMock();
        _discountCodeRepo.GetQueryable().Returns(emptyQuery);
        _messageService.Get("RecordNotFound").Returns("Code not found.");

        var result = await _sut.ValidateCodeAsync(request);

        Assert.True(result.IsSuccess);
        Assert.False(result.Data!.IsValid);
        Assert.Equal("Code not found.", result.Data.ErrorMessage);
    }

    [Fact]
    public async Task ValidateCodeAsync_ReturnsInvalid_WhenExpired()
    {
        var request = new ValidateDiscountCodeRequest { Code = "EXPIRED", OrderAmount = 100 };
        var expiredCode = new DiscountCode { Code = "EXPIRED", StartDate = DateTime.UtcNow.AddDays(-10), EndDate = DateTime.UtcNow.AddDays(-1) };
        var mockQuery = new List<DiscountCode> { expiredCode }.AsQueryable().BuildMock();
        _discountCodeRepo.GetQueryable().Returns(mockQuery);
        _messageService.Get("DiscountCodeExpired").Returns("Discount code has expired.");

        var result = await _sut.ValidateCodeAsync(request);

        Assert.True(result.IsSuccess);
        Assert.False(result.Data!.IsValid);
        Assert.Equal("Discount code has expired.", result.Data.ErrorMessage);
    }

    [Fact]
    public async Task ValidateCodeAsync_ReturnsValidResult_WhenCodeIsValid()
    {
        var request = new ValidateDiscountCodeRequest { Code = "VALID20", OrderAmount = 100 };
        var validCode = new DiscountCode { Code = "VALID20", DiscountValue = 20, StartDate = DateTime.UtcNow.AddDays(-1), EndDate = DateTime.UtcNow.AddDays(10) };
        var mockQuery = new List<DiscountCode> { validCode }.AsQueryable().BuildMock();
        _discountCodeRepo.GetQueryable().Returns(mockQuery);

        var result = await _sut.ValidateCodeAsync(request);

        Assert.True(result.IsSuccess);
        Assert.True(result.Data!.IsValid);
        Assert.Equal(20, result.Data.CalculatedDiscountAmount);
        Assert.Equal(80, result.Data.FinalAmount);
    }
}
