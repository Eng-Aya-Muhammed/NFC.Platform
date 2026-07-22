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
        private readonly IGenericRepository<CardPricing> _cardPricingRepo;
        private readonly IGenericRepository<UserProfile> _userProfileRepo;

        private readonly Hangfire.IBackgroundJobClient _backgroundJobClient;
        private readonly ICardPricingService _cardPricingService;
        private readonly CardOrderService _sut;

        public CardOrderServiceTests()
        {
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _mapper = Substitute.For<IMapper>();
            _messageService = Substitute.For<IMessageService>();
            _currentTenant = Substitute.For<ICurrentTenant>();
            _excelParser = Substitute.For<IExcelParser>();
            
            _otpSettingsOptions = Substitute.For<IOptions<OtpSettings>>();
            _otpSettingsOptions.Value.Returns(new OtpSettings { CooldownSeconds = 60, MaxResendAttempts = 5 });

            _orderRepo = Substitute.For<IGenericRepository<CardOrder>>();

            _orderItemRepo = Substitute.For<IGenericRepository<CardOrderItem>>();
            _jobRepo = Substitute.For<IGenericRepository<EmployeeImportJob>>();
            _cardPricingRepo = Substitute.For<IGenericRepository<CardPricing>>();
            _userProfileRepo = Substitute.For<IGenericRepository<UserProfile>>();

            _unitOfWork.Repository<CardOrder>().Returns(_orderRepo);

            _unitOfWork.Repository<CardOrderItem>().Returns(_orderItemRepo);
            _unitOfWork.Repository<EmployeeImportJob>().Returns(_jobRepo);
            _unitOfWork.Repository<CardPricing>().Returns(_cardPricingRepo);
            _unitOfWork.Repository<UserProfile>().Returns(_userProfileRepo);

            _userProfileRepo.GetQueryable().Returns(new List<UserProfile>().AsQueryable().BuildMock());

            var defaultPricings = new List<CardPricing>
            {
                new() { CardType = CardType.Plastic, UnitPrice = 4.5m, Currency = "KWD", IsActive = true, EffectiveFrom = DateTime.UtcNow.AddDays(-10) },
                new() { CardType = CardType.Wooden, UnitPrice = 6.0m, Currency = "KWD", IsActive = true, EffectiveFrom = DateTime.UtcNow.AddDays(-10) },
                new() { CardType = CardType.Metal, UnitPrice = 8.5m, Currency = "KWD", IsActive = true, EffectiveFrom = DateTime.UtcNow.AddDays(-10) }
            };
            var mockPricingQueryable = defaultPricings.AsQueryable().BuildMock();
            _cardPricingRepo.GetQueryable().Returns(mockPricingQueryable);

            var validator = Substitute.For<FluentValidation.IValidator<CreateCardOrderRequest>>();
            var validationResult = new FluentValidation.Results.ValidationResult();
            validator.ValidateAsync(Arg.Any<CreateCardOrderRequest>(), default)
                .Returns(Task.FromResult(validationResult));


            _backgroundJobClient = Substitute.For<Hangfire.IBackgroundJobClient>();

            _messageService.Get(default!, default!).ReturnsForAnyArgs(x => (string)x[0]);

            _mapper.Map<CardOrder>(Arg.Any<CardOrder>()).Returns(x =>
            {
                var src = x.Arg<CardOrder>();
                if (src == null) return null!;
                return new CardOrder
                {
                    CardName = src.CardName,
                    CardType = src.CardType,
                    CardDesignType = src.CardDesignType,
                    FrontDesignUrl = src.FrontDesignUrl,
                    BackDesignUrl = src.BackDesignUrl,

                };
            });

            _cardPricingService = Substitute.For<ICardPricingService>();
            _cardPricingService.CalculateOrderPricingAsync(Arg.Any<CardType>(), Arg.Any<int>())
                .Returns(ServiceResult<OrderPricingResponseDto>.Success(new OrderPricingResponseDto { UnitPrice = 4.5m, TotalPrice = 4.5m, Currency = "KWD" }));

            _sut = new CardOrderService(_unitOfWork, _mapper, _messageService, _currentTenant, _cardPricingService, validator, _backgroundJobClient, _otpSettingsOptions);
        }

        // ── GetByIdAsync ──────────────────────────────────────────────────────────

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
            _mapper.Map<CardOrderDto>(order).Returns(dto);

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
            _mapper.Map<CardOrderDto>(Arg.Any<CardOrder>()).Returns(new CardOrderDto());

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
            _mapper.Map<CardOrderDto>(Arg.Any<CardOrder>()).Returns(new CardOrderDto());

            // Act
            var result = await _sut.GetPagedOrdersAsync(request, "Encoding");

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal(1, result.Data.TotalCount);
        }

        // ── CreateAsync ───────────────────────────────────────────────────────────

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

            var request = new CreateCardOrderRequest { Quantity = 5, CardType = CardType.Metal };
            var order = new CardOrder { Id = Guid.NewGuid(), Quantity = 5, CardType = CardType.Metal, Items = [] };
            _mapper.Map<CardOrder>(request).Returns(order);

            var currentUser = new User { Id = userId, AccountType = AccountType.Individual };
            _unitOfWork.Repository<User>().GetQueryable().Returns(new List<User> { currentUser }.AsQueryable().BuildMock());

            var createdQueryable = new List<CardOrder> { order }.AsQueryable().BuildMock();
            _orderRepo.GetQueryable().Returns(createdQueryable);

            var dto = new CardOrderDto { Quantity = 5, CardType = CardType.Metal };
            _mapper.Map<CardOrderDto>(order).Returns(dto);
            var pricingResp = new OrderPricingResponseDto { UnitPrice = 8.5m, TotalPrice = 42.5m };
            _cardPricingService.CalculateOrderPricingAsync(Arg.Any<CardType>(), Arg.Any<int>()).Returns(ServiceResult<OrderPricingResponseDto>.Success(pricingResp));

            // Act
            var result = await _sut.CreateOrderAsync(request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(200, result.StatusCode);
            Assert.Equal(8.5m, order.UnitPrice);
            Assert.Equal(42.5m, order.TotalPrice);
        }

        [Fact]
        public async Task CreateAsync_QueuesHangfireJob_WhenCompanyAdmin_AndExcelDataUrlProvided()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _currentTenant.UserId.Returns(userId);
            _currentTenant.TenantId.Returns(Guid.NewGuid());

            var request = new CreateCardOrderRequest 
            { 
                Quantity = 10, 
                CardType = CardType.Plastic,
                ExcelDataUrl = "https://example.com/employees.xlsx" 
            };
            
            // Mock pricing
            var pricing = new CardPricing { CardType = CardType.Plastic, UnitPrice = 5, IsActive = true, EffectiveFrom = DateTime.UtcNow.AddDays(-1) };
            _unitOfWork.Repository<CardPricing>().GetQueryable().Returns(new List<CardPricing> { pricing }.AsQueryable().BuildMock());

            // Mock User to return CompanyAdmin
            var currentUser = new User { Id = userId, AccountType = AccountType.CompanyAdmin };
            _unitOfWork.Repository<User>().GetQueryable().Returns(new List<User> { currentUser }.AsQueryable().BuildMock());
            
            // Mock Order retrieval for returning DTO
            var orderRepo = Substitute.For<IGenericRepository<CardOrder>>();
            orderRepo.GetQueryable().Returns(new List<CardOrder> { new CardOrder { Id = Guid.NewGuid(), Items = [] } }.AsQueryable().BuildMock());
            _unitOfWork.Repository<CardOrder>().Returns(orderRepo);
            
            _mapper.Map<CardOrder>(request).Returns(new CardOrder { Quantity = request.Quantity, CardType = request.CardType, CardDesignType = request.CardDesignType, ExcelDataUrl = request.ExcelDataUrl });
            _mapper.Map<CardOrderDto>(Arg.Any<CardOrder>()).Returns(new CardOrderDto());

            // Act
            var result = await _sut.CreateOrderAsync(request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(200, result.StatusCode);
            
            // Verify EmployeeImportJob was added
            await _unitOfWork.Repository<EmployeeImportJob>().Received(1).AddAsync(Arg.Is<EmployeeImportJob>(j => 
                j.ExcelFileUrl == "https://example.com/employees.xlsx" && 
                j.UserId == userId));
                
            // Verify Hangfire enqueue
            _backgroundJobClient.Received(1).Create(Arg.Any<Hangfire.Common.Job>(), Arg.Any<Hangfire.States.EnqueuedState>());
        }

        [Fact]
        public async Task CreateAsync_DoesNotQueueHangfireJob_WhenIndividualUser_AndExcelDataUrlProvided()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _currentTenant.UserId.Returns(userId);
            _currentTenant.TenantId.Returns(Guid.NewGuid());

            var request = new CreateCardOrderRequest 
            { 
                Quantity = 1, 
                CardType = CardType.Plastic,
                ExcelDataUrl = "https://example.com/individual_should_not_use_this.xlsx" 
            };
            
            // Mock pricing
            var pricing = new CardPricing { CardType = CardType.Plastic, UnitPrice = 5, IsActive = true, EffectiveFrom = DateTime.UtcNow.AddDays(-1) };
            _unitOfWork.Repository<CardPricing>().GetQueryable().Returns(new List<CardPricing> { pricing }.AsQueryable().BuildMock());

            // Mock User to return Individual
            var currentUser = new User { Id = userId, AccountType = AccountType.Individual };
            _unitOfWork.Repository<User>().GetQueryable().Returns(new List<User> { currentUser }.AsQueryable().BuildMock());
            
            // Mock Order retrieval for returning DTO
            var orderRepo = Substitute.For<IGenericRepository<CardOrder>>();
            orderRepo.GetQueryable().Returns(new List<CardOrder> { new CardOrder { Id = Guid.NewGuid(), Items = [] } }.AsQueryable().BuildMock());
            _unitOfWork.Repository<CardOrder>().Returns(orderRepo);
            
            _mapper.Map<CardOrder>(request).Returns(new CardOrder { Quantity = request.Quantity, CardType = request.CardType, CardDesignType = request.CardDesignType, ExcelDataUrl = request.ExcelDataUrl });
            _mapper.Map<CardOrderDto>(Arg.Any<CardOrder>()).Returns(new CardOrderDto());

            // Act
            var result = await _sut.CreateOrderAsync(request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(200, result.StatusCode);
            
            // Verify EmployeeImportJob was NOT added
            await _unitOfWork.Repository<EmployeeImportJob>().DidNotReceive().AddAsync(Arg.Any<EmployeeImportJob>());
                
            // Verify Hangfire enqueue was NOT called
            _backgroundJobClient.DidNotReceive().Create(Arg.Any<Hangfire.Common.Job>(), Arg.Any<Hangfire.States.EnqueuedState>());
        }

        [Fact]
        public async Task CreateReorderAsync_Returns422_WhenDeliveryIsCourierAndNoShippingAddress()
        {
            // Arrange
            var parentId = Guid.NewGuid();
            _currentTenant.UserId.Returns(Guid.NewGuid());
            var parentOrder = new CardOrder { Id = parentId, CardType = CardType.Plastic };
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


        // ── DeleteAsync ───────────────────────────────────────────────────────────

        [Fact]
        public async Task DeleteAsync_ReturnsNotFound_WhenOrderDoesNotExist()
        {
            // Arrange
            var id = Guid.NewGuid();
            _orderRepo.GetByIdAsync(id).Returns((CardOrder?)null);
            _messageService.Get("RecordNotFound").Returns("Record not found.");

            // Act
            var result = await _sut.DeleteOrderAsync(id);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
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
            var parentOrder = new CardOrder { Id = parentId, CardType = CardType.Plastic };
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
            var parentOrder = new CardOrder { Id = parentId, CardType = CardType.Plastic, CardName = "Parent Card" };
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
            var parentOrder = new CardOrder { Id = parentId, CardType = CardType.Plastic, CardName = "Parent Card" };
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
            _mapper.Map<CardOrderItem>(employees[0]).Returns(item1);
            _mapper.Map<CardOrderItem>(employees[1]).Returns(item2);

            // Act
            var result = await _sut.CreateReorderAsync(parentId, request);

            // Assert
            Assert.True(result.IsSuccess);
            await _orderRepo.Received(1).AddAsync(Arg.Is<CardOrder>(o =>
                o.ParentOrderId == parentId &&
                o.Quantity == 2 &&
                o.Items.Count == 2 &&
                o.Items.Contains(item1) &&
                o.Items.Contains(item2)));
            await _unitOfWork.Received(1).SaveChangesAsync();
        }

        [Fact]
        public async Task CreateReorderAsync_Returns422_WhenSpecificEmployeeNotFound()
        {
            // Arrange
            var parentId = Guid.NewGuid();
            _currentTenant.UserId.Returns(Guid.NewGuid());
            var parentOrder = new CardOrder { Id = parentId, CardType = CardType.Plastic };
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
            var parentOrder = new CardOrder { Id = parentId, CardType = CardType.Plastic };
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
            var parentOrder = new CardOrder { Id = parentId, CardType = CardType.Plastic, CardName = "Parent Card" };
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
            _mapper.Map<CardOrderItem>(employees[0]).Returns(item1);
            _mapper.Map<CardOrderItem>(employees[1]).Returns(item2);

            // Act
            var result = await _sut.CreateReorderAsync(parentId, request);

            // Assert
            Assert.True(result.IsSuccess);
            await _orderRepo.Received(1).AddAsync(Arg.Is<CardOrder>(o =>
                o.ParentOrderId == parentId &&
                o.Quantity == 2 &&
                o.Items.Count == 2 &&
                o.Items.Contains(item1) &&
                o.Items.Contains(item2)));
            await _unitOfWork.Received(1).SaveChangesAsync();
        }











        [Fact]
        public async Task CreateReorderAsync_RegressionTest_UsesSharedHelperToBuildItems()
        {
            // Arrange
            var parentId = Guid.NewGuid();
            _currentTenant.UserId.Returns(Guid.NewGuid());
            var parentOrder = new CardOrder { Id = parentId, CardType = CardType.Plastic };
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

            var pricing = new CardPricing { CardType = CardType.Plastic, UnitPrice = 4.5m, Currency = "KWD", IsActive = true, EffectiveFrom = DateTime.UtcNow.AddDays(-10) };
            _cardPricingRepo.GetQueryable().Returns(new List<CardPricing> { pricing }.AsQueryable().BuildMock());

            var reorder = new CardOrder { Id = Guid.NewGuid(), CardType = CardType.Plastic };
            _mapper.Map<CardOrder>(parentOrder).Returns(reorder);
            _mapper.Map<CardOrderDto>(reorder).Returns(new CardOrderDto());

            // Act
            var result = await _sut.CreateReorderAsync(parentId, request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(200, result.StatusCode);
            Assert.Single(reorder.Items);
        }

        // ── OTP Resend Unit Tests ──────────────────────────────────────────────

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
    }
}
