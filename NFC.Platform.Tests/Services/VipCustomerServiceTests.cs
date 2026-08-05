using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using MockQueryable.NSubstitute;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Application.DTOs.VipCustomer;
using NFC.Platform.Application.Interfaces.Repositories;
using NFC.Platform.Application.Mapping;
using NFC.Platform.Application.Services;
using NFC.Platform.Domain.Entities;
using NFC.Platform.Domain.Enums;
using NSubstitute;
using Xunit;

namespace NFC.Platform.Tests.Services;

public class VipCustomerServiceTests
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly VipCustomerService _vipCustomerService;

    public VipCustomerServiceTests()
    {
        _unitOfWork = Substitute.For<IUnitOfWork>();

        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<VipCustomerMappingProfile>();
        });
        _mapper = config.CreateMapper();

        _vipCustomerService = new VipCustomerService(_unitOfWork, _mapper);
    }

    [Fact]
    public async Task GetPublicVipCustomersAsync_ReturnsCombinedSortedList()
    {
        var company = new Company { Id = Guid.NewGuid(), Name = "Google", LogoUrl = "https://logo.com/google.png", IsVip = true, VipDisplayOrder = 1 };
        var profile = new UserProfile { Id = Guid.NewGuid(), FullName = "John Doe", ProfilePictureUrl = "https://img.com/john.png", EmployeeId = null, IsVip = true, VipDisplayOrder = 2 };

        var companyRepo = Substitute.For<IGenericRepository<Company>>();
        companyRepo.GetQueryable().Returns(new List<Company> { company }.AsQueryable().BuildMock());

        var profileRepo = Substitute.For<IGenericRepository<UserProfile>>();
        profileRepo.GetQueryable().Returns(new List<UserProfile> { profile }.AsQueryable().BuildMock());

        _unitOfWork.Repository<Company>().Returns(companyRepo);
        _unitOfWork.Repository<UserProfile>().Returns(profileRepo);

        var result = await _vipCustomerService.GetPublicVipCustomersAsync();

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data.Count);

        var first = result.Data.First();
        Assert.Equal("Google", first.Name);
        Assert.Equal(VipCustomerType.Company, first.CustomerType);

        var second = result.Data.Last();
        Assert.Equal("John Doe", second.Name);
        Assert.Equal(VipCustomerType.Individual, second.CustomerType);
    }

    [Fact]
    public async Task GetPublicVipCustomersAsync_FiltersOutNonVipAndEmployeeProfiles()
    {
        var vipCompany = new Company { Id = Guid.NewGuid(), Name = "Spotify", LogoUrl = "https://logo.com/spotify.png", IsVip = true, VipDisplayOrder = 1 };
        var nonVipCompany = new Company { Id = Guid.NewGuid(), Name = "Other", IsVip = false, VipDisplayOrder = 2 };

        var vipStandalone = new UserProfile { Id = Guid.NewGuid(), FullName = "Alice", EmployeeId = null, IsVip = true, VipDisplayOrder = 3 };
        var vipEmployee = new UserProfile { Id = Guid.NewGuid(), FullName = "Bob (Employee)", EmployeeId = Guid.NewGuid(), IsVip = true, VipDisplayOrder = 4 };

        var companyRepo = Substitute.For<IGenericRepository<Company>>();
        companyRepo.GetQueryable().Returns(new List<Company> { vipCompany, nonVipCompany }.AsQueryable().BuildMock());

        var profileRepo = Substitute.For<IGenericRepository<UserProfile>>();
        profileRepo.GetQueryable().Returns(new List<UserProfile> { vipStandalone, vipEmployee }.AsQueryable().BuildMock());

        _unitOfWork.Repository<Company>().Returns(companyRepo);
        _unitOfWork.Repository<UserProfile>().Returns(profileRepo);

        var result = await _vipCustomerService.GetPublicVipCustomersAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data.Count);
        Assert.Contains(result.Data, x => x.Name == "Spotify");
        Assert.Contains(result.Data, x => x.Name == "Alice");
        Assert.DoesNotContain(result.Data, x => x.Name == "Other");
        Assert.DoesNotContain(result.Data, x => x.Name == "Bob (Employee)");
    }

    [Fact]
    public async Task GetAdminVipCustomersAsync_ReturnsPagedCombinedList()
    {
        var company = new Company { Id = Guid.NewGuid(), Name = "Google", IsVip = true, VipDisplayOrder = 1 };
        var profile = new UserProfile { Id = Guid.NewGuid(), FullName = "Alice", EmployeeId = null, IsVip = true, VipDisplayOrder = 2 };

        var companyRepo = Substitute.For<IGenericRepository<Company>>();
        companyRepo.GetQueryable().Returns(new List<Company> { company }.AsQueryable().BuildMock());

        var profileRepo = Substitute.For<IGenericRepository<UserProfile>>();
        profileRepo.GetQueryable().Returns(new List<UserProfile> { profile }.AsQueryable().BuildMock());

        _unitOfWork.Repository<Company>().Returns(companyRepo);
        _unitOfWork.Repository<UserProfile>().Returns(profileRepo);

        var pagination = new PaginationRequest { PageNumber = 1, PageSize = 10 };

        var result = await _vipCustomerService.GetAdminVipCustomersAsync(pagination);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data.TotalCount);
        Assert.Equal(2, result.Data.Items.Count);
    }
}
