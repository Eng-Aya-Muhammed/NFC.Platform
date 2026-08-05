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
            _currentTenant.UserId.Returns(Guid.NewGuid());
            _currentTenant.TenantId.Returns(Guid.NewGuid());
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
            _userProfileRepo.GetQueryable().Returns(new List<UserProfile>().AsQueryable().BuildMock());

            var cardPackageRepo = Substitute.For<IGenericRepository<CardPackage>>();
            cardPackageRepo.GetByIdAsync(Arg.Any<Guid>()).Returns(callInfo => new CardPackage { Id = callInfo.Arg<Guid>(), IsActive = true, NumberOfCards = 100, Price = 50.0m });
            _unitOfWork.Repository<CardPackage>().Returns(cardPackageRepo);

            var cardDesignRepo = Substitute.For<IGenericRepository<CardDesign>>();
            cardDesignRepo.GetQueryable().Returns(new List<CardDesign> { new CardDesign { Id = Guid.Empty, IsPaid = true, TotalQuantity = 100, UsedQuantity = 0 } }.AsQueryable().BuildMock());
            _unitOfWork.Repository<CardDesign>().Returns(cardDesignRepo);

            var defaultUserRepo = Substitute.For<IGenericRepository<User>>();
            defaultUserRepo.GetQueryable().Returns(new List<User>().AsQueryable().BuildMock());
            _unitOfWork.Repository<User>().Returns(defaultUserRepo);

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
            var request = new CreateCardOrderRequest { CardDesignId = Guid.NewGuid(), Quantity = 1 };

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

            var designId = Guid.NewGuid();
            var cardPackageId = Guid.NewGuid();
            var cardDesign = new CardDesign
            {
                Id = designId,
                IsPaid = true,
                TotalQuantity = 10,
                UsedQuantity = 0,
                CardPackageId = cardPackageId,
                UnitPrice = 42.5m,
                TotalPrice = 42.5m,
                Currency = "KWD"
            };

            var request = new CreateCardOrderRequest { CardDesignId = designId, Quantity = 5 };
            var order = new CardOrder { Id = Guid.NewGuid(), Quantity = 5, CardDesignId = designId, Items = [] };

            var currentUser = new User { Id = userId, AccountType = AccountType.Individual };
            _unitOfWork.Repository<User>().GetQueryable().Returns(new List<User> { currentUser }.AsQueryable().BuildMock());

            var designRepo = Substitute.For<IGenericRepository<CardDesign>>();
            designRepo.GetQueryable().Returns(new List<CardDesign> { cardDesign }.AsQueryable().BuildMock());
            _unitOfWork.Repository<CardDesign>().Returns(designRepo);

            var createdQueryable = new List<CardOrder> { order }.AsQueryable().BuildMock();
            _orderRepo.GetQueryable().Returns(createdQueryable);

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
        public async Task CreateAsync_Returns422_WhenCompanyOrderMissingAssignmentScope()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tenantId = Guid.NewGuid();
            var designId = Guid.NewGuid();
            _currentTenant.UserId.Returns(userId);
            _currentTenant.TenantId.Returns(tenantId);

            var design = new CardDesign { Id = designId, IsPaid = true, TotalQuantity = 10, UsedQuantity = 0 };
            var designRepo = Substitute.For<IGenericRepository<CardDesign>>();
            designRepo.GetQueryable().Returns(new List<CardDesign> { design }.AsQueryable().BuildMock());
            _unitOfWork.Repository<CardDesign>().Returns(designRepo);

            var userRepo = Substitute.For<IGenericRepository<User>>();
            userRepo.GetQueryable().Returns(new List<User> { new User { Id = userId, AccountType = AccountType.CompanyAdmin } }.AsQueryable().BuildMock());
            _unitOfWork.Repository<User>().Returns(userRepo);

            var request = new CreateCardOrderRequest 
            { 
                CardDesignId = designId,
                AssignmentScope = null
            };

            // Act
            var result = await _sut.CreateOrderAsync(request);

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

            var request = new UpdateCardOrderRequest { Notes = "New Notes" };

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

            var request = new UpdateCardOrderRequest { Notes = "New Notes" };

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
                Quantity = 5,
                UnitPrice = 4.5m,
                TotalPrice = 22.5m
            };
            _orderRepo.GetQueryable().Returns(new List<CardOrder> { order }.AsQueryable().BuildMock());

            var request = new UpdateCardOrderRequest { Notes = "New Notes" };

            // Act
            var result = await _sut.UpdateOrderAsync(id, request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("New Notes", order.Notes);
            Assert.Equal(5, order.Quantity);
            Assert.Equal(22.5m, order.TotalPrice); // Unchanged
            await _unitOfWork.Received(1).SaveChangesAsync();
            await _unitOfWork.Received(1).BeginTransactionAsync();
            await _unitOfWork.Received(1).CommitTransactionAsync();
        }

        [Fact]
        public async Task UpdateOrderAsync_ThrowsEmployeeCountMismatch_WhenQuantityBypassed()
        {
            // Arrange
            var id = Guid.NewGuid();
            var tenantId = Guid.NewGuid();
            _currentTenant.TenantId.Returns((Guid?)tenantId);
            var designId = Guid.NewGuid();
            var order = new CardOrder
            {
                Id = id,
                TenantId = tenantId,
                CardDesignId = designId,
                Status = OrderStatus.PendingReview,
                Quantity = 5,
                Items = new List<CardOrderItem>()
            };

            var cardDesign = new CardDesign { Id = designId, TotalQuantity = 10, UsedQuantity = 0 };
            var designRepo = Substitute.For<IGenericRepository<CardDesign>>();
            designRepo.GetQueryable().Returns(new List<CardDesign> { cardDesign }.AsQueryable().BuildMock());
            designRepo.GetByIdAsync(designId).Returns(cardDesign);
            _unitOfWork.Repository<CardDesign>().Returns(designRepo);

            var request = new UpdateCardOrderRequest
            {
                AssignmentScope = AssignmentScope.SpecificEmployees,
                EmployeeIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() } // Only 2 employees provided but Quantity is 10
            };

            var empRepo = Substitute.For<IGenericRepository<Employee>>();
            empRepo.GetQueryable().Returns(new List<Employee>().AsQueryable().BuildMock());
            _unitOfWork.Repository<Employee>().Returns(empRepo);

            var queryable = new List<CardOrder> { order }.AsQueryable().BuildMock();
            _orderRepo.GetQueryable().Returns(queryable);

            // Act
            var result = await _sut.UpdateOrderAsync(id, request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(422, result.StatusCode);
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
            var parentOrder = new CardOrder { Id = parentId };
            _orderRepo.GetQueryable().Returns(new List<CardOrder> { parentOrder }.AsQueryable().BuildMock());

            var empRepo = Substitute.For<IGenericRepository<Employee>>();
            empRepo.GetQueryable().Returns(new List<Employee>().AsQueryable().BuildMock());
            _unitOfWork.Repository<Employee>().Returns(empRepo);

            var request = new ReorderRequest
            {
                AssignmentScope = AssignmentScope.SpecificEmployees,
                EmployeeIds = new List<Guid> { Guid.NewGuid() }
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
            var parentOrder = new CardOrder { Id = parentId };
            _orderRepo.GetQueryable().Returns(new List<CardOrder> { parentOrder }.AsQueryable().BuildMock());

            var request = new ReorderRequest { AssignmentScope = AssignmentScope.Individual };

            // Act
            var result = await _sut.CreateReorderAsync(parentId, request);

            // Assert
            Assert.True(result.IsSuccess);
            await _orderRepo.Received(1).AddAsync(Arg.Is<CardOrder>(o => o.ParentOrderId == parentId));
            await _unitOfWork.Received(1).SaveChangesAsync();
        }

        [Fact]
        public async Task CreateReorderAsync_ReturnsSuccess_WithItems_WhenAssignmentScopeIsSpecificEmployees()
        {
            // Arrange
            var parentId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            _currentTenant.UserId.Returns(userId);
            var parentOrder = new CardOrder { Id = parentId };
            _orderRepo.GetQueryable().Returns(new List<CardOrder> { parentOrder }.AsQueryable().BuildMock());

            var employeeId1 = Guid.NewGuid();
            var employeeId2 = Guid.NewGuid();
            var request = new ReorderRequest
            {
                AssignmentScope = AssignmentScope.SpecificEmployees,
                EmployeeIds = new List<Guid> { employeeId1, employeeId2 }
            };

            var userProfile1 = new UserProfile { Id = Guid.NewGuid(), Phone = "123456" };
            var userProfile2 = new UserProfile { Id = Guid.NewGuid(), Phone = "654321" };
            var mockEmployees = new List<Employee>
            {
                new Employee { Id = employeeId1, FullName = "Emp 1", IsDeleted = false, UserProfile = userProfile1 },
                new Employee { Id = employeeId2, FullName = "Emp 2", IsDeleted = false, UserProfile = userProfile2 }
            };

            var employeeRepo = Substitute.For<IGenericRepository<Employee>>();
            employeeRepo.GetQueryable().Returns(mockEmployees.AsQueryable().BuildMock());
            _unitOfWork.Repository<Employee>().Returns(employeeRepo);

            // Act
            var result = await _sut.CreateReorderAsync(parentId, request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(200, result.StatusCode);
            await _orderRepo.Received(1).AddAsync(Arg.Is<CardOrder>(o =>
                o.ParentOrderId == parentId &&
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
            var parentOrder = new CardOrder { Id = parentId };
            _orderRepo.GetQueryable().Returns(new List<CardOrder> { parentOrder }.AsQueryable().BuildMock());

            var employeeId = Guid.NewGuid();
            var request = new ReorderRequest
            {
                AssignmentScope = AssignmentScope.SpecificEmployees,
                EmployeeIds = new List<Guid> { employeeId }
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
            var parentOrder = new CardOrder { Id = parentId };
            _orderRepo.GetQueryable().Returns(new List<CardOrder> { parentOrder }.AsQueryable().BuildMock());

            var employeeId = Guid.NewGuid();
            var request = new ReorderRequest
            {
                AssignmentScope = AssignmentScope.SpecificEmployees,
                EmployeeIds = new List<Guid> { employeeId }
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
            var parentOrder = new CardOrder { Id = parentId };
            _orderRepo.GetQueryable().Returns(new List<CardOrder> { parentOrder }.AsQueryable().BuildMock());

            var request = new ReorderRequest
            {
                AssignmentScope = AssignmentScope.AllEmployees
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

            // Act
            var result = await _sut.CreateReorderAsync(parentId, request);

            // Assert
            Assert.True(result.IsSuccess);
            await _orderRepo.Received(1).AddAsync(Arg.Is<CardOrder>(o =>
                o.ParentOrderId == parentId &&
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
            var parentOrder = new CardOrder { Id = parentId };
            _orderRepo.GetQueryable().Returns(new List<CardOrder> { parentOrder }.AsQueryable().BuildMock());

            var request = new ReorderRequest
            {
                AssignmentScope = AssignmentScope.AllEmployees
            };

            var mockEmployees = new List<Employee>
            {
                new Employee { Id = Guid.NewGuid(), FullName = "Emp 1", IsDeleted = false, UserProfile = new UserProfile { Id = Guid.NewGuid() } }
            };
            var employeeRepo = Substitute.For<IGenericRepository<Employee>>();
            employeeRepo.GetQueryable().Returns(mockEmployees.AsQueryable().BuildMock());
            _unitOfWork.Repository<Employee>().Returns(employeeRepo);

            var reorder = new CardOrder { Id = Guid.NewGuid() };

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
                DeliveryOtpHash      = NFC.Platform.BuildingBlocks.Common.Helpers.OtpHasher.HashOtp("111111"),
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
            Assert.NotEqual(NFC.Platform.BuildingBlocks.Common.Helpers.OtpHasher.HashOtp("111111"), order.DeliveryOtpHash); // New OTP hash generated
            Assert.NotNull(order.DeliveryOtpHash);
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
        public async Task CreateDesignAsync_Returns400_WhenExcelParsingFails()
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

            var unitPackage = new CardPackage { Id = Guid.NewGuid(), NumberOfCards = 1, Price = 10, IsActive = true };
            var packageRepo = Substitute.For<IGenericRepository<CardPackage>>();
            packageRepo.GetQueryable().Returns(new List<CardPackage> { unitPackage }.AsQueryable().BuildMock());
            _unitOfWork.Repository<CardPackage>().Returns(packageRepo);

            var cardDesignService = new CardDesignService(
                _unitOfWork,
                _mapper,
                _messageService,
                _currentTenant,
                _employeeService,
                Substitute.For<IConfiguration>()
            );

            var cardTypeId = Guid.NewGuid();
            var cardTypeRepo = Substitute.For<IGenericRepository<CardType>>();
            cardTypeRepo.GetByIdAsync(cardTypeId).Returns(new CardType { Id = cardTypeId, IsActive = true });
            _unitOfWork.Repository<CardType>().Returns(cardTypeRepo);

            var request = new CreateCardDesignRequest
            {
                CardTypeId = cardTypeId,
                CustomQuantity = 10,
                ExcelDataUrl = "https://example.com/invalid-file.xlsx"
            };

            // Act
            var result = await cardDesignService.CreateDesignAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(400, result.StatusCode);
            Assert.Contains(result.Errors, e => e.Contains("FailedToParseExcel"));
            
            // Ensure no design was saved
            await _unitOfWork.DidNotReceive().SaveChangesAsync();
        }

        [Fact]
        public async Task GetPagedOrdersAsync_FiltersBySearch_MatchesItemEmployeeName()
        {
            // Arrange
            var order1 = new CardOrder
            {
                Id = Guid.NewGuid(),
                TrackingNumber = "TRK12345",
                Items = new List<CardOrderItem> { new() { EmployeeName = "Ziad Khaled", Email = "ziad@test.com" } }
            };
            var order2 = new CardOrder
            {
                Id = Guid.NewGuid(),
                TrackingNumber = "TRK99999",
                Items = new List<CardOrderItem> { new() { EmployeeName = "Omar Hassan", Email = "omar@test.com" } }
            };

            var queryable = new List<CardOrder> { order1, order2 }.AsQueryable().BuildMock();
            _orderRepo.GetQueryable().Returns(queryable);

            var request = new PaginationRequest { PageNumber = 1, PageSize = 10 };

            // Act
            var result = await _sut.GetPagedOrdersAsync(request, null, "Ziad");

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal(1, result.Data.TotalCount);
            Assert.Equal(order1.Id, result.Data.Items.First().Id);
        }

        [Fact]
        public async Task GetPagedOrdersAsync_HandlesNullNavigationProperties_WithoutCrashing()
        {
            // Arrange — Order with null TrackingNumber, null Notes, null CardDesign, null Item Email/Phone
            var orderWithNulls = new CardOrder
            {
                Id = Guid.NewGuid(),
                TrackingNumber = null,
                Notes = null,
                CardDesign = null,
                Items = new List<CardOrderItem> { new() { EmployeeName = "Test Emp", Email = null, Phone = null } }
            };

            var queryable = new List<CardOrder> { orderWithNulls }.AsQueryable().BuildMock();
            _orderRepo.GetQueryable().Returns(queryable);

            var request = new PaginationRequest { PageNumber = 1, PageSize = 10 };

            // Act — Search for term that doesn't match
            var result = await _sut.GetPagedOrdersAsync(request, null, "NonExistentSearchTerm");

            // Assert — Must return 0 count without throwing NullReferenceException
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal(0, result.Data.TotalCount);
        }

        [Fact]
        public async Task GetPagedOrdersAsync_WhenSearchNullOrEmpty_ReturnsAllRecords()
        {
            // Arrange
            var o1 = new CardOrder { Id = Guid.NewGuid() };
            var o2 = new CardOrder { Id = Guid.NewGuid() };

            var queryable = new List<CardOrder> { o1, o2 }.AsQueryable().BuildMock();
            _orderRepo.GetQueryable().Returns(queryable);

            var request = new PaginationRequest { PageNumber = 1, PageSize = 10 };

            // Act
            var resultWithNull = await _sut.GetPagedOrdersAsync(request, null, null);
            var resultWithWhitespace = await _sut.GetPagedOrdersAsync(request, null, "   ");

            // Assert — Search ignored, returns all records
            Assert.True(resultWithNull.IsSuccess);
            Assert.Equal(2, resultWithNull.Data!.TotalCount);
            Assert.True(resultWithWhitespace.IsSuccess);
            Assert.Equal(2, resultWithWhitespace.Data!.TotalCount);
        }
    }
}
