using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Options;
using MockQueryable.NSubstitute;
using NSubstitute;
using NFC.Platform.Application.DTOs.CardOrder;
using NFC.Platform.Application.DTOs.Settings;
using NFC.Platform.Application.Interfaces.Services;
using NFC.Platform.Application.Mapping;
using NFC.Platform.Application.Services;
using NFC.Platform.Application.Validators.CardOrder;
using NFC.Platform.Application.Interfaces.Repositories;
using NFC.Platform.BuildingBlocks.Localization;
using NFC.Platform.Domain.Entities;
using NFC.Platform.Domain.Enums;
using Xunit;

namespace NFC.Platform.Tests.Services
{
    public class CardOrderFullLifecycleTests
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IMessageService _messageService;
        private readonly ICurrentTenant _currentTenant;
        private readonly IValidator<CreateCardOrderRequest> _createValidator;
        private readonly IValidator<UpdateCardOrderRequest> _updateValidator;
        private readonly Hangfire.IBackgroundJobClient _backgroundJobClient;
        private readonly IEmployeeService _employeeService;
        private readonly IOptions<OtpSettings> _otpSettingsOptions;

        private readonly CardOrderService _sut;

        private readonly Guid _tenantId = Guid.NewGuid();
        private readonly Guid _userId = Guid.NewGuid();
        private readonly Guid _companyId = Guid.NewGuid();

        public CardOrderFullLifecycleTests()
        {
            _unitOfWork = Substitute.For<IUnitOfWork>();
            var mapperConfig = new MapperConfiguration(cfg => cfg.AddProfile(new CardOrderMappingProfile()));
            _mapper = mapperConfig.CreateMapper();
            _messageService = Substitute.For<IMessageService>();
            _currentTenant = Substitute.For<ICurrentTenant>();
            _currentTenant.TenantId.Returns(_tenantId);
            _currentTenant.UserId.Returns(_userId);

            _createValidator = Substitute.For<IValidator<CreateCardOrderRequest>>();
            _createValidator.ValidateAsync(Arg.Any<CreateCardOrderRequest>(), default)
                .Returns(Task.FromResult(new ValidationResult()));

            _updateValidator = new UpdateCardOrderRequestValidator(_messageService);

            _backgroundJobClient = Substitute.For<Hangfire.IBackgroundJobClient>();
            _employeeService = Substitute.For<IEmployeeService>();

            _otpSettingsOptions = Substitute.For<IOptions<OtpSettings>>();
            _otpSettingsOptions.Value.Returns(new OtpSettings { CooldownSeconds = 60, MaxResendAttempts = 5 });

            _messageService.Get(Arg.Any<string>(), Arg.Any<object[]>()).Returns(call =>
            {
                var key = call.Arg<string>();
                var args = call.Arg<object[]>();
                if (args != null && args.Length > 0)
                {
                    return $"{key}:{string.Join(",", args)}";
                }
                return key;
            });

            // Mock Repositories
            SetupMockRepositories();

            _sut = new CardOrderService(
                _unitOfWork,
                _mapper,
                _messageService,
                _currentTenant,
                _createValidator,
                _updateValidator,
                _backgroundJobClient,
                _employeeService,
                _otpSettingsOptions);
        }

        private void SetupMockRepositories()
        {
            var company = new Company { Id = _companyId, TenantId = _tenantId, Name = "Test Company" };
            var companyRepo = Substitute.For<IGenericRepository<Company>>();
            companyRepo.GetQueryable().Returns(new List<Company> { company }.AsQueryable().BuildMock());
            _unitOfWork.Repository<Company>().Returns(companyRepo);

            var ordersList = new List<CardOrder>();
            var orderRepo = Substitute.For<IGenericRepository<CardOrder>>();
            orderRepo.AddAsync(Arg.Do<CardOrder>(o => ordersList.Add(o))).Returns(Task.CompletedTask);
            orderRepo.GetQueryable().Returns(x => ordersList.AsQueryable().BuildMock());
            _unitOfWork.Repository<CardOrder>().Returns(orderRepo);

            var itemRepo = Substitute.For<IGenericRepository<CardOrderItem>>();
            itemRepo.GetQueryable().Returns(new List<CardOrderItem>().AsQueryable().BuildMock());
            _unitOfWork.Repository<CardOrderItem>().Returns(itemRepo);

            var typeRepo = Substitute.For<IGenericRepository<CardType>>();
            typeRepo.GetQueryable().Returns(new List<CardType>().AsQueryable().BuildMock());
            _unitOfWork.Repository<CardType>().Returns(typeRepo);

            var pkgRepo = Substitute.For<IGenericRepository<CardPackage>>();
            pkgRepo.GetQueryable().Returns(new List<CardPackage>().AsQueryable().BuildMock());
            _unitOfWork.Repository<CardPackage>().Returns(pkgRepo);

            var empRepo = Substitute.For<IGenericRepository<Employee>>();
            empRepo.GetQueryable().Returns(new List<Employee>().AsQueryable().BuildMock());
            _unitOfWork.Repository<Employee>().Returns(empRepo);

            var userRepo = Substitute.For<IGenericRepository<User>>();
            userRepo.GetQueryable().Returns(new List<User> { new User { Id = _userId, AccountType = AccountType.Individual } }.AsQueryable().BuildMock());
            _unitOfWork.Repository<User>().Returns(userRepo);
        }

        // ==========================================
        // 1. CREATE ORDER TESTS
        // ==========================================

        [Fact]
        public async Task CreateOrderAsync_Fails_WhenDesignNotFound()
        {
            // Arrange
            var designId = Guid.NewGuid();
            var designRepo = Substitute.For<IGenericRepository<CardDesign>>();
            designRepo.GetQueryable().Returns(new List<CardDesign>().AsQueryable().BuildMock());
            _unitOfWork.Repository<CardDesign>().Returns(designRepo);

            var request = new CreateCardOrderRequest
            {
                CardDesignId = designId,
                Quantity = 1
            };

            // Act
            var result = await _sut.CreateOrderAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
            Assert.Equal("DesignNotFound", result.Message);
        }

        [Fact]
        public async Task CreateOrderAsync_Fails_WhenQuantityExceedsDesignCapacity()
        {
            // Arrange
            var designId = Guid.NewGuid();
            var design = new CardDesign { Id = designId, IsPaid = true, TotalQuantity = 5, UsedQuantity = 0, CardPackageId = Guid.NewGuid(), UnitPrice = 10, TotalPrice = 50, Currency = "KWD" };
            var designRepo = Substitute.For<IGenericRepository<CardDesign>>();
            designRepo.GetQueryable().Returns(new List<CardDesign> { design }.AsQueryable().BuildMock());
            _unitOfWork.Repository<CardDesign>().Returns(designRepo);

            var empIds = Enumerable.Range(0, 6).Select(_ => Guid.NewGuid()).ToList();
            var employees = empIds.Select(id => new Employee { Id = id, FullName = "Emp", UserProfile = new UserProfile() }).ToList();
            var empRepo = Substitute.For<IGenericRepository<Employee>>();
            empRepo.GetQueryable().Returns(employees.AsQueryable().BuildMock());
            _unitOfWork.Repository<Employee>().Returns(empRepo);

            var userRepo = Substitute.For<IGenericRepository<User>>();
            userRepo.GetQueryable().Returns(new List<User> { new User { Id = _userId, AccountType = AccountType.CompanyAdmin } }.AsQueryable().BuildMock());
            _unitOfWork.Repository<User>().Returns(userRepo);

            var request = new CreateCardOrderRequest
            {
                CardDesignId = designId,
                AssignmentScope = AssignmentScope.SpecificEmployees,
                EmployeeIds = empIds,
                QuantityPerEmployee = 1
            };

            // Act
            var result = await _sut.CreateOrderAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(400, result.StatusCode);
            Assert.Contains("DesignRemainingQuantityExceeded", result.Message);
        }

        [Fact]
        public async Task CreateOrderAsync_Succeeds_WithPackageCapacityAndEmployees()
        {
            // Arrange
            var designId = Guid.NewGuid();
            var typeId = Guid.NewGuid();
            var design = new CardDesign { Id = designId, CardTypeId = typeId, IsPaid = true, TotalQuantity = 10, UsedQuantity = 0, CardPackageId = Guid.NewGuid(), UnitPrice = 100, TotalPrice = 1000, Currency = "KWD" };
            var type = new CardType { Id = typeId, IsActive = true };

            var designRepo = Substitute.For<IGenericRepository<CardDesign>>();
            designRepo.GetQueryable().Returns(new List<CardDesign> { design }.AsQueryable().BuildMock());
            _unitOfWork.Repository<CardDesign>().Returns(designRepo);

            var typeRepo = Substitute.For<IGenericRepository<CardType>>();
            typeRepo.GetByIdAsync(typeId).Returns(type);
            _unitOfWork.Repository<CardType>().Returns(typeRepo);

            var userRepo = Substitute.For<IGenericRepository<User>>();
            userRepo.GetQueryable().Returns(new List<User> { new User { Id = _userId, AccountType = AccountType.CompanyAdmin } }.AsQueryable().BuildMock());
            _unitOfWork.Repository<User>().Returns(userRepo);

            var empIds = Enumerable.Range(0, 3).Select(_ => Guid.NewGuid()).ToList();
            var employees = empIds.Select(id => new Employee { Id = id, FullName = "Emp", UserProfile = new UserProfile() }).ToList();
            var empRepo = Substitute.For<IGenericRepository<Employee>>();
            empRepo.GetQueryable().Returns(employees.AsQueryable().BuildMock());
            _unitOfWork.Repository<Employee>().Returns(empRepo);

            var request = new CreateCardOrderRequest
            {
                CardDesignId = designId,
                AssignmentScope = AssignmentScope.SpecificEmployees,
                EmployeeIds = empIds,
                QuantityPerEmployee = 1
            };

            // Act
            var result = await _sut.CreateOrderAsync(request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(200, result.StatusCode);
            Assert.NotNull(result.Data);
            Assert.Equal(3, result.Data.Quantity);
            Assert.Equal(1000, result.Data.TotalPrice);
            Assert.Equal(100, result.Data.UnitPrice);
        }

        // ==========================================
        // 2. REORDER TESTS
        // ==========================================

        [Fact]
        public async Task CreateReorderAsync_Fails_WhenParentOrderNotFound()
        {
            // Arrange
            var parentId = Guid.NewGuid();
            var orderRepo = Substitute.For<IGenericRepository<CardOrder>>();
            orderRepo.GetQueryable().Returns(new List<CardOrder>().AsQueryable().BuildMock());
            _unitOfWork.Repository<CardOrder>().Returns(orderRepo);

            var request = new ReorderRequest
            {
                AssignmentScope = AssignmentScope.AllEmployees
            };

            // Act
            var result = await _sut.CreateReorderAsync(parentId, request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task CreateReorderAsync_Succeeds_UsingParentCardPackage()
        {
            _currentTenant.UserId.Returns(_userId);
            _currentTenant.TenantId.Returns(_tenantId);

            var userRepo = Substitute.For<IGenericRepository<User>>();
            userRepo.GetQueryable().Returns(new List<User> { new User { Id = _userId, AccountType = AccountType.Individual } }.AsQueryable().BuildMock());
            _unitOfWork.Repository<User>().Returns(userRepo);

            var parentId = Guid.NewGuid();
            var pkgId = Guid.NewGuid();
            var designId = Guid.NewGuid();
            var cardDesign = new CardDesign { Id = designId, TenantId = _tenantId, CardPackageId = pkgId, IsPaid = true, TotalQuantity = 20, UsedQuantity = 10, UnitPrice = 50, TotalPrice = 500, Currency = "KWD" };
            var parentOrder = new CardOrder
            {
                Id = parentId,
                TenantId = _tenantId,
                CardDesignId = designId,
                CardDesign = cardDesign,
                Status = OrderStatus.Approved,
                UnitPrice = 50,
                TotalPrice = 500,
                Quantity = 10
            };

            var pkg = new CardPackage { Id = pkgId, IsActive = true, NumberOfCards = 10, Price = 500 };

            var ordersList = new List<CardOrder> { parentOrder };
            var orderRepo = Substitute.For<IGenericRepository<CardOrder>>();
            orderRepo.AddAsync(Arg.Do<CardOrder>(o => ordersList.Add(o))).Returns(Task.CompletedTask);
            orderRepo.GetQueryable().Returns(_ => ordersList.AsQueryable().BuildMock());
            _unitOfWork.Repository<CardOrder>().Returns(orderRepo);

            var designRepo = Substitute.For<IGenericRepository<CardDesign>>();
            designRepo.GetByIdAsync(designId).Returns(cardDesign);
            designRepo.GetQueryable().Returns(new List<CardDesign> { cardDesign }.AsQueryable().BuildMock());
            _unitOfWork.Repository<CardDesign>().Returns(designRepo);

            var pkgRepo = Substitute.For<IGenericRepository<CardPackage>>();
            pkgRepo.GetByIdAsync(pkgId).Returns(pkg);
            _unitOfWork.Repository<CardPackage>().Returns(pkgRepo);

            var request = new ReorderRequest
            {
                AssignmentScope = AssignmentScope.Individual
            };

            // Act
            var result = await _sut.CreateReorderAsync(parentId, request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal(10, result.Data.Quantity);
            Assert.Equal(500, result.Data.TotalPrice);
        }

        // ==========================================
        // 3. UPDATE ORDER TESTS
        // ==========================================

        [Fact]
        public async Task UpdateOrderAsync_Fails_WhenOrderNotInPendingReviewStatus()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var order = new CardOrder
            {
                Id = orderId,
                TenantId = _tenantId,
                Status = OrderStatus.InPrinting,
                Items = []
            };

            var orderRepo = Substitute.For<IGenericRepository<CardOrder>>();
            orderRepo.GetQueryable().Returns(new List<CardOrder> { order }.AsQueryable().BuildMock());
            _unitOfWork.Repository<CardOrder>().Returns(orderRepo);

            var request = new UpdateCardOrderRequest
            {
                Notes = "Updated notes"
            };

            // Act
            var result = await _sut.UpdateOrderAsync(orderId, request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(400, result.StatusCode);
            Assert.Equal("OrderCannotBeUpdated", result.Message);
        }

        [Fact]
        public async Task UpdateOrderAsync_Succeeds_WhenUpdatingNotesAndQuantity()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var designId = Guid.NewGuid();

            var order = new CardOrder
            {
                Id = orderId,
                TenantId = _tenantId,
                CardDesignId = designId,
                Status = OrderStatus.PendingReview,
                Quantity = 5,
                TotalPrice = 250,
                Items = []
            };

            var design = new CardDesign { Id = designId, IsPaid = true, TotalQuantity = 50, UsedQuantity = 5 };

            var orderRepo = Substitute.For<IGenericRepository<CardOrder>>();
            orderRepo.GetQueryable().Returns(new List<CardOrder> { order }.AsQueryable().BuildMock());
            _unitOfWork.Repository<CardOrder>().Returns(orderRepo);

            var designRepo = Substitute.For<IGenericRepository<CardDesign>>();
            designRepo.GetQueryable().Returns(new List<CardDesign> { design }.AsQueryable().BuildMock());
            _unitOfWork.Repository<CardDesign>().Returns(designRepo);

            var request = new UpdateCardOrderRequest
            {
                Quantity = 10,
                Notes = "Upgraded to 10 cards"
            };

            // Act
            var result = await _sut.UpdateOrderAsync(orderId, request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(200, result.StatusCode);
            Assert.Equal(10, order.Quantity);
        }
    }
}
