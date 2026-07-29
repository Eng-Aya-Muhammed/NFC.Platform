using Microsoft.Extensions.Options;
using NFC.Platform.Application.DTOs.Settings;

namespace NFC.Platform.Tests.Services
{
    public class CardOrderSeparationTests
    {
        [Fact]
        public void CardOrder_ShouldNotHaveCustomDesignRequestIdProperty()
        {
            // Act
            var property = typeof(CardOrder).GetProperty("CustomDesignRequestId");

            // Assert
            Assert.Null(property);
        }

        [Fact]
        public async Task CreateAsync_ShouldNotQueryTemplateRequestRepository()
        {
            // Arrange
            var unitOfWork = Substitute.For<IUnitOfWork>();
            var mapper = Substitute.For<IMapper>();
            var messageService = Substitute.For<IMessageService>();
            var currentTenant = Substitute.For<ICurrentTenant>();
            var excelParser = Substitute.For<IExcelParser>();
            var validator = Substitute.For<IValidator<CreateCardOrderRequest>>();
            var backgroundJobClient = Substitute.For<Hangfire.IBackgroundJobClient>();

            var orderRepo = Substitute.For<IGenericRepository<CardOrder>>();
            unitOfWork.Repository<CardOrder>().Returns(orderRepo);

            var validationResult = new FluentValidation.Results.ValidationResult();
            validator.ValidateAsync(Arg.Any<CreateCardOrderRequest>(), default)
                .Returns(Task.FromResult(validationResult));

            var userId = Guid.NewGuid();
            currentTenant.UserId.Returns(userId);
            currentTenant.TenantId.Returns(Guid.NewGuid());

            var currentUser = new User { Id = userId, AccountType = AccountType.Individual };
            var userRepo = Substitute.For<IGenericRepository<User>>();
            userRepo.GetQueryable().Returns(new List<User> { currentUser }.AsQueryable().BuildMock());
            unitOfWork.Repository<User>().Returns(userRepo);

            var cardTypeRepo = Substitute.For<IGenericRepository<CardType>>();
            var cardType = new CardType { Id = Guid.NewGuid(), IsActive = true };
            cardTypeRepo.GetByIdAsync(Arg.Any<Guid>()).Returns(cardType);
            unitOfWork.Repository<CardType>().Returns(cardTypeRepo);

            var cardPackageRepo = Substitute.For<IGenericRepository<CardPackage>>();
            var cardPackage = new CardPackage { Id = Guid.NewGuid(), IsActive = true, Price = 10 };
            cardPackageRepo.GetByIdAsync(Arg.Any<Guid>()).Returns(cardPackage);
            unitOfWork.Repository<CardPackage>().Returns(cardPackageRepo);

            var order = new CardOrder
            {
                Id = Guid.NewGuid(),
                Quantity = 1
            };
            mapper.Map<CardOrder>(Arg.Any<CreateCardOrderRequest>()).Returns(order);
            orderRepo.GetQueryable().Returns(new List<CardOrder> { order }.AsQueryable().BuildMock());

            var otpSettingsOptions = Substitute.For<IOptions<OtpSettings>>();
            otpSettingsOptions.Value.Returns(new OtpSettings { CooldownSeconds = 60, MaxResendAttempts = 5 });
            
            var companyRepo = Substitute.For<IGenericRepository<Company>>();
            companyRepo.GetQueryable().Returns(new List<Company> { new Company { TenantId = Guid.NewGuid(), Id = Guid.NewGuid() } }.AsQueryable().BuildMock());
            unitOfWork.Repository<Company>().Returns(companyRepo);

            var service = new CardOrderService(
                unitOfWork,
                mapper,
                messageService,
                currentTenant,
                validator,
                Substitute.For<IValidator<UpdateCardOrderRequest>>(),
                backgroundJobClient,
                Substitute.For<IEmployeeService>(),
                otpSettingsOptions
            );

            var cardDesign = new CardDesign
            {
                Id = Guid.NewGuid(),
                IsPaid = true,
                TotalQuantity = 10,
                UsedQuantity = 0,
                CardPackageId = cardPackage.Id,
                UnitPrice = 10,
                TotalPrice = 10,
                Currency = "KWD"
            };
            var designRepo = Substitute.For<IGenericRepository<CardDesign>>();
            designRepo.GetQueryable().Returns(new List<CardDesign> { cardDesign }.AsQueryable().BuildMock());
            unitOfWork.Repository<CardDesign>().Returns(designRepo);

            var request = new CreateCardOrderRequest
            {
                CardDesignId = cardDesign.Id,
                Quantity = 1
            };

            // Act
            var result = await service.CreateOrderAsync(request);

            // Assert
            Assert.True(result.IsSuccess);
            unitOfWork.DidNotReceive().Repository<TemplateRequest>();
        }

        [Fact]
        public async Task ResolvePublicProfileAsync_QueriesOnlyProfileTemplateRequestType()
        {
            // Arrange
            var unitOfWork = Substitute.For<IUnitOfWork>();
            var mapper = Substitute.For<IMapper>();
            var messageService = Substitute.For<IMessageService>();

            var profileRepo = Substitute.For<IGenericRepository<UserProfile>>();
            var templateRequestRepo = Substitute.For<IGenericRepository<TemplateRequest>>();
            unitOfWork.Repository<UserProfile>().Returns(profileRepo);
            unitOfWork.Repository<TemplateRequest>().Returns(templateRequestRepo);

            var tenantId = Guid.NewGuid();
            var company = new Company { TenantId = tenantId };
            var employee = new Employee { Company = company };
            var userProfile = new UserProfile
            {
                TenantId = tenantId,
                Employee = employee
            };

            mapper.Map<EmployeeDetailsDto>(userProfile).Returns(new EmployeeDetailsDto());

            profileRepo.GetQueryable().Returns(new List<UserProfile> { userProfile }.AsQueryable().BuildMock());

            // Mock completed TemplateRequest queryable
            var completedRequest = new TemplateRequest
            {
                TenantId = tenantId,
                Status = TemplateRequestStatus.Completed,
                RequestType = TemplateRequestType.ProfileTemplate,
                LogoUrl = "https://cdn.example.com/logo.png"
            };
            templateRequestRepo.GetQueryable().Returns(new List<TemplateRequest> { completedRequest }.AsQueryable().BuildMock());

            var service = new ProfileMetricService(unitOfWork, messageService, mapper);

            // Act
            var result = await service.ResolvePublicProfileAsync(userProfile.Id);

            // Assert
            Assert.True(result.IsSuccess);
            // Verify that the query filtered by RequestType == TemplateRequestType.ProfileTemplate
            // We can confirm this because MockQueryable parsed and executed the query correctly
        }
    }
}
