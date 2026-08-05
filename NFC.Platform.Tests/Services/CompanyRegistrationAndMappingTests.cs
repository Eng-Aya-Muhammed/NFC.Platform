using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AutoMapper;
using Hangfire;
using MockQueryable.NSubstitute;
using NFC.Platform.Application.DTOs.Auth;
using NFC.Platform.Application.DTOs.Company;
using NFC.Platform.Application.Interfaces;
using NFC.Platform.Application.Interfaces.Services;
using NFC.Platform.Application.Mapping;
using NFC.Platform.Application.Services;
using NFC.Platform.BuildingBlocks.Common;
using NFC.Platform.BuildingBlocks.Localization;
using NFC.Platform.Domain.Entities;
using NFC.Platform.Domain.Enums;
using NSubstitute;
using Xunit;

namespace NFC.Platform.Tests.Services
{
    public class CompanyRegistrationAndMappingTests
    {
        private readonly IMapper _mapper;

        public CompanyRegistrationAndMappingTests()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<CompanyMappingProfile>();
                cfg.AddProfile<AuthMappingProfile>();
            });
            _mapper = config.CreateMapper();
        }

        [Fact]
        public void AutoMapper_Maps_RegisterRequest_To_Company_With_All_Figma_Fields()
        {
            var registerRequest = new RegisterRequest
            {
                AccountType = AccountType.CompanyAdmin,
                CompanyName = "OnPoint General Trading Co.",
                Address = "Kuwait City, Al-Hamra Tower",
                IndustryType = "Technology & Contracting",
                CompanySize = CompanySize.Medium,
                CommercialRegistrationNumber = "CR-987654321",
                Email = "admin@onpoint.com",
                Phone = "+96512345678"
            };

            var company = _mapper.Map<Company>(registerRequest);

            Assert.NotNull(company);
            Assert.Equal("OnPoint General Trading Co.", company.Name);
            Assert.Equal("Kuwait City, Al-Hamra Tower", company.Address);
            Assert.Equal("Technology & Contracting", company.Activity);
            Assert.Equal("Technology & Contracting", company.IndustryType);
            Assert.Equal(CompanySize.Medium, company.CompanySize);
            Assert.Equal("CR-987654321", company.CommercialRegistry);
            Assert.Equal("CR-987654321", company.CommercialRegistrationNumber);
        }

        [Fact]
        public void AutoMapper_Maps_UpdateCompanyProfileRequest_To_Company_Without_Data_Loss()
        {
            var existingCompany = new Company
            {
                Name = "Old Name",
                Activity = "Old Activity",
                CommercialRegistry = "Old CR",
                Address = "Old Address",
                CompanySize = CompanySize.Small
            };

            var updateRequest = new UpdateCompanyProfileRequest
            {
                Name = "Updated OnPoint Co.",
                IndustryType = "Software Engineering",
                CommercialRegistrationNumber = "CR-11223344",
                Address = "New Kuwait City Tower",
                CompanySize = CompanySize.Large
            };

            _mapper.Map(updateRequest, existingCompany);

            Assert.Equal("Updated OnPoint Co.", existingCompany.Name);
            Assert.Equal("Software Engineering", existingCompany.Activity);
            Assert.Equal("Software Engineering", existingCompany.IndustryType);
            Assert.Equal("CR-11223344", existingCompany.CommercialRegistry);
            Assert.Equal("CR-11223344", existingCompany.CommercialRegistrationNumber);
            Assert.Equal("New Kuwait City Tower", existingCompany.Address);
            Assert.Equal(CompanySize.Large, existingCompany.CompanySize);
        }

        [Fact]
        public void AutoMapper_Maps_Company_To_CompanyProfileDto_With_Figma_Aliases()
        {
            var company = new Company
            {
                Id = Guid.NewGuid(),
                Name = "OnPoint Co.",
                Activity = "Fintech",
                CommercialRegistry = "CR-555666",
                CompanySize = CompanySize.Enterprise,
                Address = "Shuwaikh Industrial",
                AdminUser = new User
                {
                    Email = "ceo@onpoint.com",
                    PhoneNumber = "+96599887766"
                }
            };

            var dto = _mapper.Map<CompanyProfileDto>(company);

            Assert.NotNull(dto);
            Assert.Equal("OnPoint Co.", dto.Name);
            Assert.Equal("Fintech", dto.Activity);
            Assert.Equal("Fintech", dto.IndustryType);
            Assert.Equal("CR-555666", dto.CommercialRegistry);
            Assert.Equal("CR-555666", dto.CommercialRegistrationNumber);
            Assert.Equal(CompanySize.Enterprise, dto.CompanySize);
            Assert.Equal("Shuwaikh Industrial", dto.Address);
            Assert.Equal("ceo@onpoint.com", dto.AdminUserEmail);
            Assert.Equal("ceo@onpoint.com", dto.Email);
            Assert.Equal("+96599887766", dto.Phone);
        }

        [Fact]
        public async Task AuthService_RegisterAsync_CreatesCompany_With_FigmaFields_And_MapsSuccessfully()
        {
            var unitOfWork = Substitute.For<IUnitOfWork>();
            var tokenService = Substitute.For<ITokenService>();
            var messageService = Substitute.For<IMessageService>();
            var jobClient = Substitute.For<IBackgroundJobClient>();

            var userRepo = Substitute.For<IGenericRepository<User>>();
            var tenantRepo = Substitute.For<IGenericRepository<Tenant>>();
            var companyRepo = Substitute.For<IGenericRepository<Company>>();
            var roleRepo = Substitute.For<IGenericRepository<Role>>();
            var userRoleRepo = Substitute.For<IGenericRepository<UserRole>>();
            var profileRepo = Substitute.For<IGenericRepository<UserProfile>>();

            unitOfWork.Repository<User>().Returns(userRepo);
            unitOfWork.Repository<Tenant>().Returns(tenantRepo);
            unitOfWork.Repository<Company>().Returns(companyRepo);
            unitOfWork.Repository<Role>().Returns(roleRepo);
            unitOfWork.Repository<UserRole>().Returns(userRoleRepo);
            unitOfWork.Repository<UserProfile>().Returns(profileRepo);

            userRepo.FindAsync(Arg.Any<Expression<Func<User, bool>>>()).Returns(new List<User>());
            roleRepo.FindAsync(Arg.Any<Expression<Func<Role, bool>>>())
                .Returns(new List<Role> { new() { Id = Guid.NewGuid(), Name = AppRole.CompanyAdmin.ToString() } });
            userRoleRepo.FindAsync(Arg.Any<Expression<Func<UserRole, bool>>>()).Returns(new List<UserRole>());

            tokenService.GenerateToken(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<Guid>(), Arg.Any<Guid?>(), Arg.Any<string>())
                .Returns("mock-company-jwt-token");

            var emailService = Substitute.For<IEmailService>();
            var configuration = Substitute.For<Microsoft.Extensions.Configuration.IConfiguration>();

            var authService = new AuthService(unitOfWork, tokenService, messageService, emailService, configuration, jobClient, _mapper);

            var registerRequest = new RegisterRequest
            {
                AccountType = AccountType.CompanyAdmin,
                CompanyName = "Figma Integrated Co.",
                Address = "Kuwait City",
                IndustryType = "Trading",
                CompanySize = CompanySize.Large,
                CommercialRegistrationNumber = "CR-777888",
                Email = "company@figma.com",
                Password = "Password123!",
                ConfirmPassword = "Password123!",
                Phone = "+96590001111"
            };

            var result = await authService.RegisterAsync(registerRequest);

            Assert.True(result.IsSuccess);
            Assert.True(result.Data);

            await companyRepo.Received(1).AddAsync(Arg.Is<Company>(c =>
                c.Name == "Figma Integrated Co." &&
                c.Address == "Kuwait City" &&
                c.Activity == "Trading" &&
                c.CompanySize == CompanySize.Large &&
                c.CommercialRegistry == "CR-777888"
            ));

            await userRepo.Received(1).AddAsync(Arg.Is<User>(u =>
                u.Email == "company@figma.com" &&
                u.PhoneNumber == "+96590001111" &&
                u.Username == "Figma Integrated Co."
            ));
        }

        [Fact]
        public async Task CompanyService_UpdateCompanyProfileAsync_Updates_All_Figma_Fields_And_AdminUser()
        {
            var unitOfWork = Substitute.For<IUnitOfWork>();
            var messageService = Substitute.For<IMessageService>();
            var currentTenant = Substitute.For<ICurrentTenant>();
            var companyRepo = Substitute.For<IGenericRepository<Company>>();

            var tenantId = Guid.NewGuid();
            currentTenant.TenantId.Returns(tenantId);

            var adminUser = new User
            {
                Id = Guid.NewGuid(),
                Email = "old@company.com",
                PhoneNumber = "+96511111111",
                UserProfile = new UserProfile { ContactEmail = "old@company.com" }
            };

            var company = new Company
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Name = "Old Company Name",
                Activity = "Old Activity",
                CommercialRegistry = "OLD-CR",
                CompanySize = CompanySize.Small,
                Address = "Old Address",
                AdminUserId = adminUser.Id,
                AdminUser = adminUser
            };

            var companyList = new List<Company> { company }.BuildMock();
            companyRepo.GetQueryable().Returns(companyList);
            unitOfWork.Repository<Company>().Returns(companyRepo);

            var subscriptionRepo = Substitute.For<IGenericRepository<UserSubscription>>();
            subscriptionRepo.GetQueryable().Returns(new List<UserSubscription>().BuildMock());
            unitOfWork.Repository<UserSubscription>().Returns(subscriptionRepo);

            var companyService = new CompanyService(unitOfWork, _mapper, messageService, currentTenant);

            var updateRequest = new UpdateCompanyProfileRequest
            {
                Name = "New Figma Company Name",
                IndustryType = "New Industry",
                CommercialRegistrationNumber = "NEW-CR-999",
                CompanySize = CompanySize.Enterprise,
                Address = "New Address Kuwait City",
                Email = "newadmin@company.com",
                Phone = "+96599998888"
            };

            var result = await companyService.UpdateCompanyProfileAsync(updateRequest);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal("New Figma Company Name", result.Data.Name);
            Assert.Equal("New Industry", result.Data.IndustryType);
            Assert.Equal("NEW-CR-999", result.Data.CommercialRegistrationNumber);
            Assert.Equal(CompanySize.Enterprise, result.Data.CompanySize);
            Assert.Equal("New Address Kuwait City", result.Data.Address);
            Assert.Equal("newadmin@company.com", result.Data.Email);
            Assert.Equal("+96599998888", result.Data.Phone);

            Assert.Equal("newadmin@company.com", adminUser.Email);
            Assert.Equal("+96599998888", adminUser.PhoneNumber);
            Assert.Equal("newadmin@company.com", adminUser.UserProfile!.ContactEmail);
        }
    }
}
