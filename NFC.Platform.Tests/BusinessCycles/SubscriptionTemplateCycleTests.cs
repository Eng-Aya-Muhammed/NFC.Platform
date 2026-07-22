using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using MockQueryable.NSubstitute;
using NFC.Platform.Application.Constants;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Application.DTOs.Admin;
using NFC.Platform.Application.DTOs.Employee;
using NFC.Platform.Application.DTOs.Profile;
using NFC.Platform.Application.DTOs.Template;
using NFC.Platform.Application.Interfaces.Repositories;
using NFC.Platform.Application.Interfaces.Services;
using NFC.Platform.Application.Services;
using NFC.Platform.BuildingBlocks.Localization;
using NFC.Platform.BuildingBlocks.Results;
using NFC.Platform.Domain.Entities;
using NFC.Platform.Domain.Enums;
using NSubstitute;
using Xunit;

namespace NFC.Platform.Tests.BusinessCycles
{
    /// <summary>
    /// This suite simulates an integration test by sharing in-memory data collections 
    /// across the mocked repositories, allowing us to test the entire business flow 
    /// across different services (TemplateRequest -> Admin -> Profile).
    /// </summary>
    public class SubscriptionTemplateCycleTests
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IMessageService _messageService;
        private readonly ICurrentTenant _currentTenant;
        private readonly IStorageService _storageService;
        private readonly Hangfire.IBackgroundJobClient _backgroundJobClient;

        // Shared "Database" Collections
        private readonly List<User> _users = new();
        private readonly List<Tenant> _tenants = new();
        private readonly List<Company> _companies = new();
        private readonly List<UserProfile> _userProfiles = new();
        private readonly List<SubscriptionPlan> _plans = new();
        private readonly List<UserSubscription> _subscriptions = new();
        private readonly List<TemplateRequest> _templateRequests = new();
        private readonly List<CardTemplate> _cardTemplates = new();
        private readonly List<SubscriptionPlanTemplate> _planTemplates = new();

        // Services
        private readonly TemplateRequestService _templateRequestService;
        private readonly AdminService _adminService;
        private readonly ProfileService _profileService;

        public SubscriptionTemplateCycleTests()
        {
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _mapper = Substitute.For<IMapper>();
            _messageService = Substitute.For<IMessageService>();
            _currentTenant = Substitute.For<ICurrentTenant>();
            _storageService = Substitute.For<IStorageService>();
            _backgroundJobClient = Substitute.For<Hangfire.IBackgroundJobClient>();

            SetupRepositories();
            SetupMapper();

            _messageService.Get(Arg.Any<string>()).Returns(x => x.Arg<string>());

            _templateRequestService = new TemplateRequestService(_unitOfWork, _mapper, _messageService, _currentTenant);
            _adminService = new AdminService(_unitOfWork, _mapper, _messageService, _storageService, _backgroundJobClient);
            _profileService = new ProfileService(_unitOfWork, _mapper, _messageService);
        }

        private void SetupRepositories()
        {
            var userRepo = Substitute.For<IGenericRepository<User>>();
            userRepo.GetQueryable().Returns(_users.AsQueryable().BuildMock());

            var tenantRepo = Substitute.For<IGenericRepository<Tenant>>();
            tenantRepo.GetQueryable().Returns(_tenants.AsQueryable().BuildMock());
            tenantRepo.GetByIdAsync(Arg.Any<Guid>()).Returns(callInfo => Task.FromResult(_tenants.FirstOrDefault(t => t.Id == callInfo.Arg<Guid>())));

            var companyRepo = Substitute.For<IGenericRepository<Company>>();
            companyRepo.GetQueryable().Returns(_companies.AsQueryable().BuildMock());

            var profileRepo = Substitute.For<IGenericRepository<UserProfile>>();
            profileRepo.GetQueryable().Returns(_userProfiles.AsQueryable().BuildMock());
            
            // Auto-add to memory when added via Repo
            profileRepo.When(x => x.AddAsync(Arg.Any<UserProfile>()))
                .Do(x => _userProfiles.Add(x.Arg<UserProfile>()));

            var planRepo = Substitute.For<IGenericRepository<SubscriptionPlan>>();
            planRepo.GetQueryable().Returns(_plans.AsQueryable().BuildMock());

            var subRepo = Substitute.For<IGenericRepository<UserSubscription>>();
            subRepo.GetQueryable().Returns(_subscriptions.AsQueryable().BuildMock());

            var requestRepo = Substitute.For<IGenericRepository<TemplateRequest>>();
            requestRepo.GetQueryable().Returns(_templateRequests.AsQueryable().BuildMock());
            requestRepo.When(x => x.AddAsync(Arg.Any<TemplateRequest>()))
                .Do(x => _templateRequests.Add(x.Arg<TemplateRequest>()));
            requestRepo.GetByIdAsync(Arg.Any<Guid>()).Returns(callInfo => Task.FromResult(_templateRequests.FirstOrDefault(t => t.Id == callInfo.Arg<Guid>())));

            var templateRepo = Substitute.For<IGenericRepository<CardTemplate>>();
            templateRepo.GetQueryable().Returns(_cardTemplates.AsQueryable().BuildMock());
            templateRepo.When(x => x.AddAsync(Arg.Any<CardTemplate>()))
                .Do(x => _cardTemplates.Add(x.Arg<CardTemplate>()));
            templateRepo.GetByIdAsync(Arg.Any<Guid>()).Returns(callInfo => Task.FromResult(_cardTemplates.FirstOrDefault(t => t.Id == callInfo.Arg<Guid>())));

            var planTemplateRepo = Substitute.For<IGenericRepository<SubscriptionPlanTemplate>>();
            planTemplateRepo.GetQueryable().Returns(_planTemplates.AsQueryable().BuildMock());
            planTemplateRepo.When(x => x.AddAsync(Arg.Any<SubscriptionPlanTemplate>()))
                .Do(x => _planTemplates.Add(x.Arg<SubscriptionPlanTemplate>()));

            _unitOfWork.Repository<User>().Returns(userRepo);
            _unitOfWork.Repository<Tenant>().Returns(tenantRepo);
            _unitOfWork.Repository<Company>().Returns(companyRepo);
            _unitOfWork.Repository<UserProfile>().Returns(profileRepo);
            _unitOfWork.Repository<SubscriptionPlan>().Returns(planRepo);
            _unitOfWork.Repository<UserSubscription>().Returns(subRepo);
            _unitOfWork.Repository<TemplateRequest>().Returns(requestRepo);
            _unitOfWork.Repository<CardTemplate>().Returns(templateRepo);
            _unitOfWork.Repository<SubscriptionPlanTemplate>().Returns(planTemplateRepo);
        }

        private void SetupMapper()
        {
            _mapper.Map<TemplateRequest>(Arg.Any<CreateTemplateRequest>()).Returns(callInfo =>
            {
                var req = callInfo.Arg<CreateTemplateRequest>();
                return new TemplateRequest { TemplateName = req.TemplateName };
            });

            _mapper.Map<TemplateRequestDto>(Arg.Any<TemplateRequest>()).Returns(callInfo =>
            {
                var req = callInfo.Arg<TemplateRequest>();
                return new TemplateRequestDto { Id = req.Id, Status = req.Status.ToString() };
            });

            _mapper.Map<EmployeeDetailsDto>(Arg.Any<User>()).Returns(callInfo =>
            {
                var u = callInfo.Arg<User>();
                return new EmployeeDetailsDto { Id = u.Id };
            });
        }

        [Fact]
        public async Task FullSubscriptionAndTemplateCycle_Success()
        {
            // =======================================================
            // 1. Arrange the Initial State
            // =======================================================
            var tenantId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var adminId = Guid.NewGuid();
            
            var plan = new SubscriptionPlan
            {
                Id = Guid.NewGuid(),
                Name = "Premium Plan",
                MaxCustomDesignRequests = 1, // Only 1 custom design allowed!
                MaxTemplateChanges = 1       // Only 1 template change allowed!
            };
            _plans.Add(plan);

            var activeSub = new UserSubscription
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                SubscriptionPlanId = plan.Id,
                SubscriptionPlan = plan,
                IsActive = true,
                EndDate = DateTime.UtcNow.AddDays(30),
                CustomDesignRequestsUsed = 0,
                TemplateChangesUsed = 0
            };
            _subscriptions.Add(activeSub);

            var tenant = new Tenant { Id = tenantId, Company = new Company { AdminUserId = adminId } };
            _tenants.Add(tenant);

            var user = new User { Id = userId, TenantId = tenantId, UserProfile = null }; // No profile yet
            _users.Add(user);

            var adminUser = new User { Id = adminId, TenantId = tenantId, Email = "admin@company.com" };
            _users.Add(adminUser);

            _currentTenant.TenantId.Returns(tenantId);

            // =======================================================
            // 2. Tenant creates a Custom Template Request
            // =======================================================
            var createRequestDto = new CreateTemplateRequest { TemplateName = "My Custom Design" };
            var createResult = await _templateRequestService.CreateRequestAsync(userId, createRequestDto);
            
            Assert.True(createResult.IsSuccess, "Tenant should be able to create a request");
            var request = _templateRequests.Single();
            
            // Link navigation properties missing from memory DB
            request.RequestedByUser = user;
            
            // Limit check: Counter should be incremented
            Assert.Equal(1, activeSub.CustomDesignRequestsUsed);

            // =======================================================
            // 3. Tenant attempts to create a SECOND request -> Fails!
            // =======================================================
            var createRequestDto2 = new CreateTemplateRequest { TemplateName = "Another Design" };
            var createResult2 = await _templateRequestService.CreateRequestAsync(userId, createRequestDto2);
            
            Assert.False(createResult2.IsSuccess, "Second request should fail because limit is 1");
            Assert.Equal(400, createResult2.StatusCode);
            Assert.Equal("CustomDesignRequestLimitReached", createResult2.Message);

            // =======================================================
            // 4. Super Admin Approves and Completes the Request
            // =======================================================
            var resolveDto = new ResolveTemplateRequestDto
            {
                Status = TemplateRequestStatus.Completed,
                Notes = "Done!"
            };

            var resolveResult = await _adminService.ResolveTemplateRequestAsync(request.Id, resolveDto);
            
            Assert.True(resolveResult.IsSuccess, "Admin should be able to resolve the request");
            Assert.Equal(TemplateRequestStatus.Completed, request.Status);

            // System should have auto-created a template
            var createdTemplate = _cardTemplates.SingleOrDefault(t => t.Name == "My Custom Design");
            Assert.NotNull(createdTemplate);
            Assert.True(createdTemplate.IsActive);

            // =======================================================
            // 5. Super Admin Assigns Template to the Tenant's Plan
            // =======================================================
            var assignResult = await _adminService.AssignTemplateAsync(plan.Id, createdTemplate.Id);
            Assert.True(assignResult.IsSuccess);

            // Verify mapping exists in memory
            var assignment = _planTemplates.Single(pt => pt.SubscriptionPlanId == plan.Id && pt.CardTemplateId == createdTemplate.Id);
            // We must explicitly attach the navigation properties since it's an in-memory test without EF tracking
            activeSub.SubscriptionPlan.PlanTemplates = new List<SubscriptionPlanTemplate> { assignment };
            assignment.CardTemplate = createdTemplate;

            // =======================================================
            // 6. Tenant assigns the Custom Template to their Profile
            // =======================================================
            var updateProfileDto = createdTemplate.Id;
            var updateResult = await _profileService.UpdateProfileTemplateAsync(userId, updateProfileDto);

            Assert.True(updateResult.IsSuccess, "Tenant should be able to assign the template to their profile");
            var profile = _userProfiles.Single(p => p.UserId == userId);
            Assert.Equal(createdTemplate.Id, profile.ProfileTemplateId);

            // Limit check: Counter should be incremented
            Assert.Equal(1, activeSub.TemplateChangesUsed);

            // =======================================================
            // 7. Tenant attempts to assign a template AGAIN -> Fails!
            // =======================================================
            var updateResult2 = await _profileService.UpdateProfileTemplateAsync(userId, updateProfileDto);
            
            Assert.False(updateResult2.IsSuccess, "Second profile template change should fail because limit is 1");
            Assert.Equal(400, updateResult2.StatusCode);
            Assert.Equal("TemplateChangeLimitReached", updateResult2.Message);

            // =======================================================
            // 8. Super Admin deletes the template -> Tenant loses it
            // =======================================================
            var deleteResult = await _adminService.DeleteTemplateAsync(createdTemplate.Id);
            
            Assert.True(deleteResult.IsSuccess);
            Assert.True(createdTemplate.IsDeleted);
            Assert.False(createdTemplate.IsActive);
            
            // The template ID should be nullified from the tenant's profile
            Assert.Null(profile.ProfileTemplateId);
        }
    }
}
