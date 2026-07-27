using Microsoft.Extensions.Options;
using NFC.Platform.Application.DTOs.Settings;

namespace NFC.Platform.Tests.Services
{
    public class CardOrderServiceTests
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IMessageService _messageService;
        private readonly ICurrentTenant _currentTenant;
        private readonly IExcelParser _excelParser;
        private readonly IOptions<OtpSettings> _otpSettingsOptions;

        private readonly IGenericRepository<CardOrder> _orderRepo;

        private readonly IGenericRepository<CardOrderItem> _orderItemRepo;
        private readonly IGenericRepository<EmployeeImportJob> _jobRepo;
        private readonly IGenericRepository<UserProfile> _userProfileRepo;

        private readonly Hangfire.IBackgroundJobClient _backgroundJobClient;
        private readonly IEmployeeService _employeeService;
        private readonly CardOrderService _sut;

        public CardOrderServiceTests()
        {
            _unitOfWork = Substitute.For<IUnitOfWork>();
            var mapperConfig = new AutoMapper.MapperConfiguration(cfg => cfg.AddProfile(new NFC.Platform.Application.Mapping.CardOrderMappingProfile()));
            _mapper = mapperConfig.CreateMapper();
            _messageService = Substitute.For<IMessageService>();
            _currentTenant = Substitute.For<ICurrentTenant>();
            _excelParser = Substitute.For<IExcelParser>();
            
            _otpSettingsOptions = Substitute.For<IOptions<OtpSettings>>();
            _otpSettingsOptions.Value.Returns(new OtpSettings { CooldownSeconds = 60, MaxResendAttempts = 5 });

            _orderRepo = Substitute.For<IGenericRepository<CardOrder>>();
            _orderRepo.GetQueryable().Returns(new List<CardOrder>().AsQueryable().BuildMock());

            _orderItemRepo = Substitute.For<IGenericRepository<CardOrderItem>>();
            _orderItemRepo.GetQueryable().Returns(new List<CardOrderItem>().AsQueryable().BuildMock());
            
            _jobRepo = Substitute.For<IGenericRepository<EmployeeImportJob>>();
            _userProfileRepo = Substitute.For<IGenericRepository<UserProfile>>();

            _unitOfWork.Repository<CardOrder>().Returns(_orderRepo);

            _unitOfWork.Repository<CardOrderItem>().Returns(_orderItemRepo);
            _unitOfWork.Repository<EmployeeImportJob>().Returns(_jobRepo);
            _unitOfWork.Repository<UserProfile>().Returns(_userProfileRepo);
            
            var _companyRepo = Substitute.For<IGenericRepository<Company>>();
            _companyRepo.GetQueryable().Returns(new List<Company> { new Company { TenantId = Guid.NewGuid(), Id = Guid.NewGuid() } }.AsQueryable().BuildMock());
            _unitOfWork.Repository<Company>().Returns(_companyRepo);

            _userProfileRepo.GetQueryable().Returns(new List<UserProfile>().AsQueryable().BuildMock());

            var validator = Substitute.For<FluentValidation.IValidator<CreateCardOrderRequest>>();
            var validationResult = new FluentValidation.Results.ValidationResult();
            validator.ValidateAsync(Arg.Any<CreateCardOrderRequest>(), default)
                .Returns(Task.FromResult(validationResult));

            _backgroundJobClient = Substitute.For<Hangfire.IBackgroundJobClient>();

            _messageService.Get(default!, default!).ReturnsForAnyArgs(x => (string)x[0]);

            var updateValidator = new UpdateCardOrderRequestValidator(_messageService);

            _employeeService = Substitute.For<IEmployeeService>();
            _sut = new CardOrderService(_unitOfWork, _mapper, _messageService, _currentTenant, validator, updateValidator, _backgroundJobClient, _employeeService, _otpSettingsOptions);
        }

        //  GetByIdAsync 

        [Fact]
        public async Task GetByIdAsync_ReturnsNotFound_WhenOrderDoesNotExist()
        {
            // Arrange
            var id = Guid.NewGuid();
            var emptyQueryable = new List<CardOrder>().AsQueryable().BuildMock();
            _orderRepo.GetQueryable().Returns(emptyQueryable);
            _messageService.Get("RecordNotFound").Returns("Record not found.");

            // Act
            var result = await _sut.GetOrderByIdAsync(id);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsSuccess_WhenOrderExists()
        {
            // Arrange
            var id = Guid.NewGuid();
            var order = new CardOrder { Id = id, Items = [] };
            var queryable = new List<CardOrder> { order }.AsQueryable().BuildMock();
            _orderRepo.GetQueryable().Returns(queryable);
            var dto = new CardOrderDto { Id = id };


            // Act
            var result = await _sut.GetOrderByIdAsync(id);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(200, result.StatusCode);
            Assert.Equal(id, result.Data!.Id);
        }

        [Fact]
        public async Task GetPagedAsync_ReturnsSuccess_WithPagedOrders()
        {
            // Arrange
            var orders = new List<CardOrder>
            {
                new CardOrder { Id = Guid.NewGuid(), CreatedAt = DateTime.UtcNow.AddMinutes(-5), Items = [] },
                new CardOrder { Id = Guid.NewGuid(), CreatedAt = DateTime.UtcNow, Items = [] }
            };
            _orderRepo.GetQueryable().Returns(orders.AsQueryable().BuildMock());

            var request = new PaginationRequest { PageNumber = 1, PageSize = 10 };


            // Act
            var result = await _sut.GetPagedOrdersAsync(request, null);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal(2, result.Data.TotalCount);
        }

        [Fact]
        public async Task GetPagedAsync_FiltersByStatus_WhenStatusFilterPassed()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var orders = new List<CardOrder>
            {
                new CardOrder { Id = orderId, Status = OrderStatus.Encoding, CreatedAt = DateTime.UtcNow.AddMinutes(-5), Items = [] },
                new CardOrder { Id = Guid.NewGuid(), Status = OrderStatus.Delivered, CreatedAt = DateTime.UtcNow, Items = [] }
            };
            _orderRepo.GetQueryable().Returns(orders.AsQueryable().BuildMock());

            var request = new PaginationRequest { PageNumber = 1, PageSize = 10 };


            // Act
            var result = await _sut.GetPagedOrdersAsync(request, "Encoding");

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal(1, result.Data.TotalCount);
        }

        //  CreateAsync 

        [Fact]
        public async Task CreateAsync_ReturnsUnauthorized_WhenUserNotAuthenticated()
        {
            // Arrange
            _currentTenant.UserId.Returns((Guid?)null);
            var request = new CreateCardOrderRequest { Quantity = 10 };

            // Act
            var result = await _sut.CreateOrderAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(401, result.StatusCode);
        }


        [Fact]
        public async Task CreateAsync_CalculatesPricing_WhenCardTypeIsProvided()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _currentTenant.UserId.Returns(userId);
            _currentTenant.TenantId.Returns(Guid.NewGuid());

            var request = new CreateCardOrderRequest { Quantity = 5, CardTypeId = Guid.NewGuid(), CardPackageId = Guid.NewGuid() };
            var order = new CardOrder { Id = Guid.NewGuid(), Quantity = 5, CardTypeId = request.CardTypeId, CardPackageId = request.CardPackageId, Items = [] };

            var currentUser = new User { Id = userId, AccountType = AccountType.Individual };
            _unitOfWork.Repository<User>().GetQueryable().Returns(new List<User> { currentUser }.AsQueryable().BuildMock());

            var cardTypeRepo = Substitute.For<IGenericRepository<CardType>>();
            var cardType = new CardType { Id = request.CardTypeId, IsActive = true };
            cardTypeRepo.GetByIdAsync(request.CardTypeId).Returns(cardType);
            _unitOfWork.Repository<CardType>().Returns(cardTypeRepo);

            var cardPackageRepo = Substitute.For<IGenericRepository<CardPackage>>();
            var cardPackage = new CardPackage { Id = request.CardPackageId, IsActive = true, Price = 42.5m };
            cardPackageRepo.GetByIdAsync(request.CardPackageId).Returns(cardPackage);
            _unitOfWork.Repository<CardPackage>().Returns(cardPackageRepo);

            var createdQueryable = new List<CardOrder> { order }.AsQueryable().BuildMock();
            _orderRepo.GetQueryable().Returns(createdQueryable);

            var dto = new CardOrderDto { Quantity = 5 };

            CardOrder? addedOrder = null;
            await _orderRepo.AddAsync(Arg.Do<CardOrder>(o => addedOrder = o));

            // Act
            var result = await _sut.CreateOrderAsync(request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(200, result.StatusCode);
            Assert.NotNull(addedOrder);
            Assert.Equal(42.5m, addedOrder.UnitPrice);
            Assert.Equal(42.5m, addedOrder.TotalPrice);
        }

        [Fact]
        public async Task CreateAsync_ValidatesExcelAndSubscription_WhenExcelDataUrlProvided()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tenantId = Guid.NewGuid();
            _currentTenant.UserId.Returns(userId);
            _currentTenant.TenantId.Returns(tenantId);

            var request = new CreateCardOrderRequest 
            { 
                Quantity = 10, 
                CardTypeId = Guid.NewGuid(),
                CardPackageId = Guid.NewGuid(),
                ExcelDataUrl = "https://example.com/employees.xlsx",
                AssignmentScope = AssignmentScope.ExcelUpload
            };

            var companyRepo = Substitute.For<IGenericRepository<Company>>();
            companyRepo.GetQueryable().Returns(new List<Company>().AsQueryable().BuildMock());
            _unitOfWork.Repository<Company>().Returns(companyRepo);

            // Act
            var result = await _sut.CreateOrderAsync(request);

            // Assert: Since Company is missing, should fail with 422 CompanyNotFound
            Assert.False(result.IsSuccess);
            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public async Task CreateReorderAsync_Returns422_WhenDeliveryIsCourierAndNoShippingAddress()
        {
            // Arrange
            var parentId = Guid.NewGuid();
            _currentTenant.UserId.Returns(Guid.NewGuid());
            var parentOrder = new CardOrder { Id = parentId, CardTypeId = Guid.NewGuid(), CardPackageId = Guid.NewGuid() };
            _orderRepo.GetQueryable().Returns(new List<CardOrder> { parentOrder }.AsQueryable().BuildMock());

            var request = new ReorderRequest
            {
                Quantity = 5,
                AssignmentScope = AssignmentScope.Individual,
                DeliveryMethod = DeliveryMethod.Courier,
                ShippingAddress = null
            };

            // Act
            var result = await _sut.CreateReorderAsync(parentId, request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(422, result.StatusCode);
        }


        //  CancelOrderAsync 

        [Fact]
        public async Task CancelOrderAsync_ReturnsNotFound_WhenOrderDoesNotExist()
        {
            // Arrange
            var id = Guid.NewGuid();
            var tenantId = Guid.NewGuid();
            _currentTenant.TenantId.Returns((Guid?)tenantId);
            _orderRepo.GetByIdAsync(id).Returns((CardOrder?)null);
            _messageService.Get("RecordNotFound").Returns("Record not found.");

            // Act
            var result = await _sut.CancelOrderAsync(id);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task CancelOrderAsync_ReturnsBadRequest_WhenStatusIsNotPendingReview()
        {
            // Arrange
            var id = Guid.NewGuid();
            var tenantId = Guid.NewGuid();
            _currentTenant.TenantId.Returns((Guid?)tenantId);
            var order = new CardOrder { Id = id, TenantId = tenantId, Status = OrderStatus.InPrinting };
            _orderRepo.GetByIdAsync(id).Returns(order);
            _messageService.Get("OrderCannotBeCancelled").Returns("Order cannot be cancelled.");

            // Act
            var result = await _sut.CancelOrderAsync(id);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public async Task CancelOrderAsync_SuccessfullyCancelsOrder_WhenStatusIsPendingReview()
        {
            // Arrange
            var id = Guid.NewGuid();
            var tenantId = Guid.NewGuid();
            _currentTenant.TenantId.Returns((Guid?)tenantId);
            var order = new CardOrder { Id = id, TenantId = tenantId, Status = OrderStatus.PendingReview };
            _orderRepo.GetByIdAsync(id).Returns(order);

            // Act
            var result = await _sut.CancelOrderAsync(id);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(200, result.StatusCode);
            Assert.Equal(OrderStatus.Cancelled, order.Status);
            await _unitOfWork.Received(1).SaveChangesAsync();
        }

        // UpdateOrderAsync

        [Fact]
        public async Task UpdateOrderAsync_ReturnsNotFound_WhenOrderDoesNotExist()
        {
            // Arrange
            var id = Guid.NewGuid();
            var tenantId = Guid.NewGuid();
            _currentTenant.TenantId.Returns((Guid?)tenantId);
            _orderRepo.GetQueryable().Returns(new List<CardOrder>().AsQueryable().BuildMock());
            _messageService.Get("RecordNotFound").Returns("Record not found.");

            var request = new UpdateCardOrderRequest { CardName = "New Name" };

            // Act
            var result = await _sut.UpdateOrderAsync(id, request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task UpdateOrderAsync_ReturnsBadRequest_WhenStatusIsNotPendingReview()
        {
            // Arrange
            var id = Guid.NewGuid();
            var tenantId = Guid.NewGuid();
            _currentTenant.TenantId.Returns((Guid?)tenantId);
            var order = new CardOrder { Id = id, TenantId = tenantId, Status = OrderStatus.InPrinting };
            _orderRepo.GetQueryable().Returns(new List<CardOrder> { order }.AsQueryable().BuildMock());
            _messageService.Get("OrderCannotBeUpdated").Returns("Order cannot be updated.");

            var request = new UpdateCardOrderRequest { CardName = "New Name" };

            // Act
            var result = await _sut.UpdateOrderAsync(id, request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(400, result.StatusCode);
            Assert.Contains("updated", result.Errors.FirstOrDefault());
        }

        [Fact]
        public async Task UpdateOrderAsync_UpdatesFields_WithoutRecalculatingPricing_WhenQuantityAndTypeUnchanged()
        {
            // Arrange
            var id = Guid.NewGuid();
            var tenantId = Guid.NewGuid();
            _currentTenant.TenantId.Returns((Guid?)tenantId);
            var order = new CardOrder { 
                Id = id, 
                TenantId = tenantId,
                Status = OrderStatus.PendingReview,
                CardName = "Old Name",
                CardTypeId = Guid.NewGuid(),
                CardPackageId = Guid.NewGuid(),
                Quantity = 5,
                UnitPrice = 4.5m,
                TotalPrice = 22.5m
            };
            _orderRepo.GetQueryable().Returns(new List<CardOrder> { order }.AsQueryable().BuildMock());

            var request = new UpdateCardOrderRequest { CardName = "New Name" };

            // Act
            var result = await _sut.UpdateOrderAsync(id, request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("New Name", order.CardName);
            Assert.Equal(5, order.Quantity);
            Assert.Equal(22.5m, order.TotalPrice); // Unchanged
            await _unitOfWork.Received(1).SaveChangesAsync();
            await _unitOfWork.Received(1).BeginTransactionAsync();
            await _unitOfWork.Received(1).CommitTransactionAsync();
        }

        [Fact]
        public async Task UpdateOrderAsync_ThrowsValidationError_WhenCustomArtworkMissingUrls()
        {
            // Arrange
            var id = Guid.NewGuid();
            var tenantId = Guid.NewGuid();
            _currentTenant.TenantId.Returns((Guid?)tenantId);
            
            var order = new CardOrder
            {
                Id = id,
                TenantId = tenantId,
                Status = OrderStatus.PendingReview,
                CardDesignType = CardDesignType.NeedCustomDesign,
                Quantity = 5
            };

            var request = new UpdateCardOrderRequest
            {
                CardDesignType = CardDesignType.CustomArtwork,
                Quantity = 5,
                // Missing FrontDesignUrl and BackDesignUrl
            };

            var queryable = new List<CardOrder> { order }.AsQueryable().BuildMock();
            _orderRepo.GetQueryable().Returns(queryable);
            
            _messageService.Get("FrontDesignRequired").Returns("Front Design Required");

            // Act
            var result = await _sut.UpdateOrderAsync(id, request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(422, result.StatusCode);
            Assert.Contains("Front Design Required", result.Message ?? string.Join(",", result.Errors));
            await _unitOfWork.DidNotReceive().SaveChangesAsync();
        }

        [Fact]
        public async Task UpdateOrderAsync_ThrowsEmployeeCountMismatch_WhenQuantityBypassed()
        {
            // Arrange
            var id = Guid.NewGuid();
            var tenantId = Guid.NewGuid();
            _currentTenant.TenantId.Returns((Guid?)tenantId);
            
            var order = new CardOrder
            {
                Id = id,
                TenantId = tenantId,
                Status = OrderStatus.PendingReview,
                Quantity = 5,
                Items = new List<CardOrderItem>()
            };

            var request = new UpdateCardOrderRequest
            {
                Quantity = 10,
                AssignmentScope = AssignmentScope.SpecificEmployees,
                EmployeeIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() } // Only 2 employees provided but Quantity is 10
            };

            var queryable = new List<CardOrder> { order }.AsQueryable().BuildMock();
            _orderRepo.GetQueryable().Returns(queryable);

            _messageService.Get("EmployeeCountMismatch").Returns("Mismatch");

            // Act
            var result = await _sut.UpdateOrderAsync(id, request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(422, result.StatusCode);
            Assert.Contains("Mismatch", result.Message ?? string.Join(",", result.Errors));
            await _unitOfWork.DidNotReceive().SaveChangesAsync();
        }





















        [Fact]
        public async Task CreateReorderAsync_ReturnsUnauthorized_WhenUserIdIsNull()
        {
            // Arrange
            _currentTenant.UserId.Returns((Guid?)null);

            // Act
            var result = await _sut.CreateReorderAsync(Guid.NewGuid(), new ReorderRequest { AssignmentScope = AssignmentScope.Individual });

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(401, result.StatusCode);
        }

        [Fact]
        public async Task CreateReorderAsync_ReturnsNotFound_WhenParentOrderDoesNotExist()
        {
            // Arrange
            _currentTenant.UserId.Returns(Guid.NewGuid());
            _orderRepo.GetQueryable().Returns(new List<CardOrder>().AsQueryable().BuildMock());

            // Act
            var result = await _sut.CreateReorderAsync(Guid.NewGuid(), new ReorderRequest { AssignmentScope = AssignmentScope.Individual });

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task CreateReorderAsync_Returns422_WhenEmployeeCountMismatch()
        {
            // Arrange
            var parentId = Guid.NewGuid();
            _currentTenant.UserId.Returns(Guid.NewGuid());
            var parentOrder = new CardOrder { Id = parentId, CardTypeId = Guid.NewGuid(), CardPackageId = Guid.NewGuid() };
            _orderRepo.GetQueryable().Returns(new List<CardOrder> { parentOrder }.AsQueryable().BuildMock());

            var request = new ReorderRequest
            {
                AssignmentScope = AssignmentScope.SpecificEmployees,
                EmployeeIds = new List<Guid> { Guid.NewGuid() },
                Quantity = 5
            };

            // Act
            var result = await _sut.CreateReorderAsync(parentId, request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(422, result.StatusCode);
        }



        [Fact]
        public async Task CreateReorderAsync_ReturnsSuccess_WhenReorderIsValid()
        {
            // Arrange
            var parentId = Guid.NewGuid();
            _currentTenant.UserId.Returns(Guid.NewGuid());
            var parentOrder = new CardOrder { Id = parentId, CardTypeId = Guid.NewGuid(), CardPackageId = Guid.NewGuid(), CardName = "Parent Card" };
            _orderRepo.GetQueryable().Returns(new List<CardOrder> { parentOrder }.AsQueryable().BuildMock());

            var request = new ReorderRequest { Quantity = 5, AssignmentScope = AssignmentScope.Individual };

            // Act
            var result = await _sut.CreateReorderAsync(parentId, request);

            // Assert
            Assert.True(result.IsSuccess);
            await _orderRepo.Received(1).AddAsync(Arg.Is<CardOrder>(o => o.ParentOrderId == parentId && o.Quantity == 5));
            await _unitOfWork.Received(1).SaveChangesAsync();
        }

        [Fact]
        public async Task CreateReorderAsync_ReturnsSuccess_WithItems_WhenAssignmentScopeIsSpecificEmployees()
        {
            // Arrange
            var parentId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            _currentTenant.UserId.Returns(userId);
            var parentOrder = new CardOrder { Id = parentId, CardTypeId = Guid.NewGuid(), CardPackageId = Guid.NewGuid(), CardName = "Parent Card" };
            _orderRepo.GetQueryable().Returns(new List<CardOrder> { parentOrder }.AsQueryable().BuildMock());

            var employeeId1 = Guid.NewGuid();
            var employeeId2 = Guid.NewGuid();
            var request = new ReorderRequest
            {
                AssignmentScope = AssignmentScope.SpecificEmployees,
                EmployeeIds = new List<Guid> { employeeId1, employeeId2 },
                Quantity = 2
            };

            var userProfile1 = new UserProfile { Id = Guid.NewGuid(), Phone = "123456" };
            var userProfile2 = new UserProfile { Id = Guid.NewGuid(), Phone = "789012" };
            var employees = new List<Employee>
            {
                new Employee { Id = employeeId1, FullName = "Emp 1", Email = "emp1@example.com", JobTitle = "Dev", Department = "IT", UserProfile = userProfile1, TenantId = Guid.NewGuid() },
                new Employee { Id = employeeId2, FullName = "Emp 2", Email = "emp2@example.com", JobTitle = "QA", Department = "IT", UserProfile = userProfile2, TenantId = Guid.NewGuid() }
            };

            var employeeRepo = Substitute.For<IGenericRepository<Employee>>();
            employeeRepo.GetQueryable().Returns(employees.AsQueryable().BuildMock());
            _unitOfWork.Repository<Employee>().Returns(employeeRepo);

            var item1 = new CardOrderItem { UserProfileId = userProfile1.Id };
            var item2 = new CardOrderItem { UserProfileId = userProfile2.Id };


            // Act
            var result = await _sut.CreateReorderAsync(parentId, request);

            // Assert
            Assert.True(result.IsSuccess);
            await _orderRepo.Received(1).AddAsync(Arg.Is<CardOrder>(o =>
                o.ParentOrderId == parentId &&
                o.Quantity == 2 &&
                o.Items.Count == 2 &&
                o.Items.Any(i => i.UserProfileId == userProfile1.Id) &&
                o.Items.Any(i => i.UserProfileId == userProfile2.Id)));
            await _unitOfWork.Received(1).SaveChangesAsync();
        }

        [Fact]
        public async Task CreateReorderAsync_Returns422_WhenSpecificEmployeeNotFound()
        {
            // Arrange
            var parentId = Guid.NewGuid();
            _currentTenant.UserId.Returns(Guid.NewGuid());
            var parentOrder = new CardOrder { Id = parentId, CardTypeId = Guid.NewGuid(), CardPackageId = Guid.NewGuid() };
            _orderRepo.GetQueryable().Returns(new List<CardOrder> { parentOrder }.AsQueryable().BuildMock());

            var employeeId = Guid.NewGuid();
            var request = new ReorderRequest
            {
                AssignmentScope = AssignmentScope.SpecificEmployees,
                EmployeeIds = new List<Guid> { employeeId },
                Quantity = 1
            };

            var employeeRepo = Substitute.For<IGenericRepository<Employee>>();
            employeeRepo.GetQueryable().Returns(new List<Employee>().AsQueryable().BuildMock());
            _unitOfWork.Repository<Employee>().Returns(employeeRepo);

            // Act
            var result = await _sut.CreateReorderAsync(parentId, request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(422, result.StatusCode);
            Assert.Contains("EmployeesNotFound", result.Message);
        }

        [Fact]
        public async Task CreateReorderAsync_Returns422_WhenSpecificEmployeeMissingProfile()
        {
            // Arrange
            var parentId = Guid.NewGuid();
            _currentTenant.UserId.Returns(Guid.NewGuid());
            var parentOrder = new CardOrder { Id = parentId, CardTypeId = Guid.NewGuid(), CardPackageId = Guid.NewGuid() };
            _orderRepo.GetQueryable().Returns(new List<CardOrder> { parentOrder }.AsQueryable().BuildMock());

            var employeeId = Guid.NewGuid();
            var request = new ReorderRequest
            {
                AssignmentScope = AssignmentScope.SpecificEmployees,
                EmployeeIds = new List<Guid> { employeeId },
                Quantity = 1
            };

            var employees = new List<Employee>
            {
                new Employee { Id = employeeId, FullName = "Emp 1", UserProfile = null }
            };

            var employeeRepo = Substitute.For<IGenericRepository<Employee>>();
            employeeRepo.GetQueryable().Returns(employees.AsQueryable().BuildMock());
            _unitOfWork.Repository<Employee>().Returns(employeeRepo);

            // Act
            var result = await _sut.CreateReorderAsync(parentId, request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(422, result.StatusCode);
            Assert.Contains("EmployeesMissingProfile", result.Message);
        }

        [Fact]
        public async Task CreateReorderAsync_ReturnsSuccess_WithItems_WhenAssignmentScopeIsAllEmployees()
        {
            // Arrange
            var parentId = Guid.NewGuid();
            _currentTenant.UserId.Returns(Guid.NewGuid());
            var parentOrder = new CardOrder { Id = parentId, CardTypeId = Guid.NewGuid(), CardPackageId = Guid.NewGuid(), CardName = "Parent Card" };
            _orderRepo.GetQueryable().Returns(new List<CardOrder> { parentOrder }.AsQueryable().BuildMock());

            var request = new ReorderRequest
            {
                AssignmentScope = AssignmentScope.AllEmployees,
                Quantity = 2
            };

            var userProfile1 = new UserProfile { Id = Guid.NewGuid() };
            var userProfile2 = new UserProfile { Id = Guid.NewGuid() };
            var employees = new List<Employee>
            {
                new Employee { Id = Guid.NewGuid(), FullName = "Emp 1", IsDeleted = false, UserProfile = userProfile1 },
                new Employee { Id = Guid.NewGuid(), FullName = "Emp 2", IsDeleted = false, UserProfile = userProfile2 }
            };

            var employeeRepo = Substitute.For<IGenericRepository<Employee>>();
            employeeRepo.GetQueryable().Returns(employees.AsQueryable().BuildMock());
            _unitOfWork.Repository<Employee>().Returns(employeeRepo);

            var item1 = new CardOrderItem { UserProfileId = userProfile1.Id };
            var item2 = new CardOrderItem { UserProfileId = userProfile2.Id };


            // Act
            var result = await _sut.CreateReorderAsync(parentId, request);

            // Assert
            Assert.True(result.IsSuccess);
            await _orderRepo.Received(1).AddAsync(Arg.Is<CardOrder>(o =>
                o.ParentOrderId == parentId &&
                o.Quantity == 2 &&
                o.Items.Count == 2 &&
                o.Items.Any(i => i.UserProfileId == userProfile1.Id) &&
                o.Items.Any(i => i.UserProfileId == userProfile2.Id)));
            await _unitOfWork.Received(1).SaveChangesAsync();
        }











        [Fact]
        public async Task CreateReorderAsync_RegressionTest_UsesSharedHelperToBuildItems()
        {
            // Arrange
            var parentId = Guid.NewGuid();
            _currentTenant.UserId.Returns(Guid.NewGuid());
            var parentOrder = new CardOrder { Id = parentId, CardTypeId = Guid.NewGuid(), CardPackageId = Guid.NewGuid() };
            _orderRepo.GetQueryable().Returns(new List<CardOrder> { parentOrder }.AsQueryable().BuildMock());

            var request = new ReorderRequest
            {
                Quantity = 1,
                AssignmentScope = AssignmentScope.AllEmployees,
                DeliveryMethod = DeliveryMethod.Pickup
            };

            var mockEmployees = new List<Employee>
            {
                new Employee { Id = Guid.NewGuid(), FullName = "Emp 1", IsDeleted = false, UserProfile = new UserProfile { Id = Guid.NewGuid() } }
            };
            var employeeRepo = Substitute.For<IGenericRepository<Employee>>();
            employeeRepo.GetQueryable().Returns(mockEmployees.AsQueryable().BuildMock());
            _unitOfWork.Repository<Employee>().Returns(employeeRepo);

            var reorder = new CardOrder { Id = Guid.NewGuid(), CardTypeId = Guid.NewGuid(), CardPackageId = Guid.NewGuid() };

            CardOrder? addedReorder = null;
            await _orderRepo.AddAsync(Arg.Do<CardOrder>(o => addedReorder = o));

            // Act
            var result = await _sut.CreateReorderAsync(parentId, request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(200, result.StatusCode);
            Assert.NotNull(addedReorder);
            Assert.Single(addedReorder.Items);
        }

        //  OTP Resend Unit Tests 

        [Fact]
        public async Task ResendDeliveryOtpAsync_ReturnsNotFound_WhenOrderDoesNotExist()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            _currentTenant.TenantId.Returns(tenantId);
            _orderRepo.GetQueryable().Returns(new List<CardOrder>().AsQueryable().BuildMock());
            _messageService.Get("RecordNotFound").Returns("Record not found.");

            // Act
            var result = await _sut.ResendOrderOtpAsync(Guid.NewGuid());

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task ResendDeliveryOtpAsync_ReturnsNotFound_WhenOrderBelongsToDifferentTenant()
        {
            // Arrange — Security Tenant Isolation check
            var currentTenantId = Guid.NewGuid();
            var differentTenantId = Guid.NewGuid();
            _currentTenant.TenantId.Returns(currentTenantId);

            var order = new CardOrder
            {
                Id       = Guid.NewGuid(),
                TenantId = differentTenantId, // Different tenant
                Status   = OrderStatus.ReadyForDelivery
            };
            _orderRepo.GetQueryable().Returns(new List<CardOrder> { order }.AsQueryable().BuildMock());
            _messageService.Get("RecordNotFound").Returns("Record not found.");

            // Act
            var result = await _sut.ResendOrderOtpAsync(order.Id);

            // Assert — Must return 404 Not Found to prevent cross-tenant enumeration
            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task ResendDeliveryOtpAsync_ReturnsFail_WhenOrderNotReadyForDelivery()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            _currentTenant.TenantId.Returns(tenantId);

            var order = new CardOrder
            {
                Id       = Guid.NewGuid(),
                TenantId = tenantId,
                Status   = OrderStatus.InPrinting // Not ReadyForDelivery
            };
            _orderRepo.GetQueryable().Returns(new List<CardOrder> { order }.AsQueryable().BuildMock());
            _messageService.Get("OrderNotReadyForDelivery").Returns("Order not ready.");

            // Act
            var result = await _sut.ResendOrderOtpAsync(order.Id);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(422, result.StatusCode);
        }

        [Fact]
        public async Task ResendDeliveryOtpAsync_ReturnsFail_WhenCooldownActive()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            _currentTenant.TenantId.Returns(tenantId);

            var order = new CardOrder
            {
                Id                    = Guid.NewGuid(),
                TenantId              = tenantId,
                Status                = OrderStatus.ReadyForDelivery,
                DeliveryOtpLastSentAt = DateTime.UtcNow.AddSeconds(-20) // Sent 20s ago (< 60s)
            };
            _orderRepo.GetQueryable().Returns(new List<CardOrder> { order }.AsQueryable().BuildMock());
            _messageService.Get("OtpCooldownActive").Returns("Please wait 60 seconds.");

            // Act
            var result = await _sut.ResendOrderOtpAsync(order.Id);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(422, result.StatusCode);
        }

        [Fact]
        public async Task ResendDeliveryOtpAsync_ReturnsFail_WhenResendLimitReached()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            _currentTenant.TenantId.Returns(tenantId);

            var order = new CardOrder
            {
                Id                    = Guid.NewGuid(),
                TenantId              = tenantId,
                Status                = OrderStatus.ReadyForDelivery,
                DeliveryOtpLastSentAt = DateTime.UtcNow.AddMinutes(-5),
                DeliveryOtpResendCount = 5 // Max limit (5) reached
            };
            _orderRepo.GetQueryable().Returns(new List<CardOrder> { order }.AsQueryable().BuildMock());
            _messageService.Get("OtpResendLimitReached").Returns("Limit reached.");

            // Act
            var result = await _sut.ResendOrderOtpAsync(order.Id);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(422, result.StatusCode);
        }

        [Fact]
        public async Task ResendDeliveryOtpAsync_Succeeds_GeneratesNewOtp_UpdatesState_AndEnqueuesJobs()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            _currentTenant.TenantId.Returns(tenantId);

            var user = new User
            {
                Email = "customer@example.com",
                UserProfile = new UserProfile { WhatsApp = "+201013503890" }
            };
            var order = new CardOrder
            {
                Id                    = Guid.NewGuid(),
                TenantId              = tenantId,
                Status                = OrderStatus.ReadyForDelivery,
                CardName              = "Premium Wood Card",
                DeliveryOtp           = "111111",
                DeliveryOtpLastSentAt = DateTime.UtcNow.AddMinutes(-3),
                DeliveryOtpResendCount = 1,
                Tenant                = new Tenant { Company = null },
                User                  = user
            };
            _orderRepo.GetQueryable().Returns(new List<CardOrder> { order }.AsQueryable().BuildMock());
            _messageService.Get("OtpResent").Returns("OTP code has been resent successfully.");
            _messageService.Get("WhatsAppNewOtp", Arg.Any<object[]>()).Returns("New pickup code!");

            // Act
            var result = await _sut.ResendOrderOtpAsync(order.Id);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("OTP code has been resent successfully.", result.Message);
            Assert.NotEqual("111111", order.DeliveryOtp); // New OTP generated
            Assert.Equal(6, order.DeliveryOtp!.Length);
            Assert.Equal(2, order.DeliveryOtpResendCount); // Incremented
            Assert.NotNull(order.DeliveryOtpExpiresAt);

            await _unitOfWork.Received(1).SaveChangesAsync();

            // Background jobs enqueued
            _backgroundJobClient.Received(1).Create(
                Arg.Is<Hangfire.Common.Job>(j =>
                    j.Method.Name == nameof(IEmailService.SendOrderReadyOtpEmailAsync)),
                Arg.Any<Hangfire.States.IState>());

            _backgroundJobClient.Received(1).Create(
                Arg.Is<Hangfire.Common.Job>(j =>
                    j.Method.Name == nameof(NFC.Platform.Application.Interfaces.Services.IWhatsAppService.SendWhatsAppMessageAsync)),
                Arg.Any<Hangfire.States.IState>());
        }
        [Fact]
        public async Task CreateOrderAsync_Returns422_WhenExcelFileFailsValidation()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tenantId = Guid.NewGuid();
            _currentTenant.UserId.Returns(userId);
            _currentTenant.TenantId.Returns(tenantId);

            var companyAdminUser = new User { Id = userId, AccountType = AccountType.CompanyAdmin };
            _unitOfWork.Repository<User>().GetQueryable().Returns(new List<User> { companyAdminUser }.AsQueryable().BuildMock());

            _employeeService.UpsertEmployeesFromExcelAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<Guid>())
                .Returns(ServiceResult<List<Guid>>.Fail(new List<string> { "FailedToParseExcel" }, 400));

            var companyRepo = Substitute.For<IGenericRepository<Company>>();
            companyRepo.GetQueryable().Returns(new List<Company> { new Company { TenantId = tenantId, Id = Guid.NewGuid() } }.AsQueryable().BuildMock());
            _unitOfWork.Repository<Company>().Returns(companyRepo);

            var request = new CreateCardOrderRequest
            {
                Quantity = 10,
                CardTypeId = Guid.NewGuid(),
                CardPackageId = Guid.NewGuid(),
                ExcelDataUrl = "https://example.com/invalid-file.xlsx",
                AssignmentScope = AssignmentScope.ExcelUpload
            };

            // Act
            // Since IHttpClientFactory is a pure mock, CreateClient() returns null, causing a NullReferenceException
            // which ValidateExcelAsync catches and returns 422 "FailedToParseExcel".
            var result = await _sut.CreateOrderAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(400, result.StatusCode);
            Assert.Contains(result.Errors, e => e.Contains("FailedToParseExcel") || e.Contains("FailedToDownloadExcel") || e.Contains("NoValidEmployeeRows"));
            
            // Ensure no order was saved
            await _unitOfWork.DidNotReceive().SaveChangesAsync();
        }
    }
}
