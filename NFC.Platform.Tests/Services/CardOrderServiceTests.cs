using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AutoMapper;
using MockQueryable.NSubstitute;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Application.Interfaces.Repositories;
using NFC.Platform.Application.Services;
using NFC.Platform.BuildingBlocks.Common.Helpers;
using NFC.Platform.BuildingBlocks.Localization;
using NFC.Platform.Domain.Entities;
using NFC.Platform.Domain.Enums;
using NSubstitute;
using Xunit;

namespace NFC.Platform.Tests.Services
{
    public class CardOrderServiceTests
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IMessageService _messageService;
        private readonly ICurrentTenant _currentTenant;
        private readonly IExcelParser _excelParser;

        private readonly IGenericRepository<CardOrder> _orderRepo;
        private readonly IGenericRepository<Card> _cardRepo;
        private readonly IGenericRepository<CardOrderItem> _orderItemRepo;
        private readonly IGenericRepository<EmployeeImportJob> _jobRepo;
        private readonly IGenericRepository<CardPricing> _cardPricingRepo;
        private readonly NFC.Platform.Application.Interfaces.Services.IStorageService _storageService;
        private readonly Hangfire.IBackgroundJobClient _backgroundJobClient;

        private readonly CardOrderService _sut;

        public CardOrderServiceTests()
        {
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _mapper = Substitute.For<IMapper>();
            _messageService = Substitute.For<IMessageService>();
            _currentTenant = Substitute.For<ICurrentTenant>();
            _excelParser = Substitute.For<IExcelParser>();

            _orderRepo = Substitute.For<IGenericRepository<CardOrder>>();
            _cardRepo = Substitute.For<IGenericRepository<Card>>();
            _orderItemRepo = Substitute.For<IGenericRepository<CardOrderItem>>();
            _jobRepo = Substitute.For<IGenericRepository<EmployeeImportJob>>();
            _cardPricingRepo = Substitute.For<IGenericRepository<CardPricing>>();

            _unitOfWork.Repository<CardOrder>().Returns(_orderRepo);
            _unitOfWork.Repository<Card>().Returns(_cardRepo);
            _unitOfWork.Repository<CardOrderItem>().Returns(_orderItemRepo);
            _unitOfWork.Repository<EmployeeImportJob>().Returns(_jobRepo);
            _unitOfWork.Repository<CardPricing>().Returns(_cardPricingRepo);

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

            _storageService = Substitute.For<NFC.Platform.Application.Interfaces.Services.IStorageService>();
            _backgroundJobClient = Substitute.For<Hangfire.IBackgroundJobClient>();

            _messageService.Get(default!, default!).ReturnsForAnyArgs(x => (string)x[0]);

            _sut = new CardOrderService(_unitOfWork, _mapper, _messageService, _currentTenant, _excelParser, validator, _storageService, _backgroundJobClient);
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
            var result = await _sut.GetByIdAsync(id);

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
            var result = await _sut.GetByIdAsync(id);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(200, result.StatusCode);
            Assert.Equal(id, result.Data!.Id);
        }

        // ── CreateAsync ───────────────────────────────────────────────────────────

        [Fact]
        public async Task CreateAsync_ReturnsUnauthorized_WhenUserNotAuthenticated()
        {
            // Arrange
            _currentTenant.UserId.Returns((Guid?)null);
            var request = new CreateCardOrderRequest { Quantity = 10 };

            // Act
            var result = await _sut.CreateAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(401, result.StatusCode);
        }

        [Fact]
        public async Task CreateAsync_ReturnsSuccess_AndSetsDefaults_WhenNoCardNameOrType()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _currentTenant.UserId.Returns(userId);

            var request = new CreateCardOrderRequest { Quantity = 5 };
            var order = new CardOrder { Id = Guid.NewGuid(), Quantity = 5, Items = [] };
            _mapper.Map<CardOrder>(request).Returns(order);

            var createdQueryable = new List<CardOrder> { order }.AsQueryable().BuildMock();
            _orderRepo.GetQueryable().Returns(createdQueryable);

            var dto = new CardOrderDto { Quantity = 5 };
            _mapper.Map<CardOrderDto>(order).Returns(dto);
            _messageService.Get("RecordCreated").Returns("Record created.");
            _messageService.Get("DefaultCardOrderName", Arg.Any<object[]>()).Returns(x => $"طلب كروت - {((object[])x[1])[0]}");

            // Act
            var result = await _sut.CreateAsync(request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(200, result.StatusCode);
            // Defaults should have been applied to the order object
            Assert.Equal($"طلب كروت - 5", order.CardName);
            Assert.Equal(CardType.Plastic, order.CardType);
            Assert.Equal(CardDesignType.BuiltInTemplate, order.CardDesignType);
            await _orderRepo.Received(1).AddAsync(order);
        }

        [Fact]
        public async Task CreateAsync_Returns422_WhenDeliveryIsCourierAndNoShippingAddress()
        {
            // Arrange
            _currentTenant.UserId.Returns(Guid.NewGuid());
            var request = new CreateCardOrderRequest 
            { 
                Quantity = 5, 
                DeliveryMethod = DeliveryMethod.Courier, 
                ShippingAddress = null 
            };

            // Act
            var result = await _sut.CreateAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(422, result.StatusCode);
        }

        [Fact]
        public async Task CreateAsync_ReturnsSuccess_WhenDeliveryIsCourierWithShippingAddress()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _currentTenant.UserId.Returns(userId);

            var request = new CreateCardOrderRequest 
            { 
                Quantity = 5, 
                DeliveryMethod = DeliveryMethod.Courier, 
                ShippingAddress = "123 Main St, Kuwait" 
            };
            var order = new CardOrder 
            { 
                Id = Guid.NewGuid(), 
                Quantity = 5, 
                DeliveryMethod = DeliveryMethod.Courier, 
                ShippingAddress = "123 Main St, Kuwait", 
                Items = [] 
            };
            _mapper.Map<CardOrder>(request).Returns(order);
            _orderRepo.GetQueryable().Returns(new List<CardOrder> { order }.AsQueryable().BuildMock());
            _mapper.Map<CardOrderDto>(order).Returns(new CardOrderDto());

            // Act
            var result = await _sut.CreateAsync(request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(200, result.StatusCode);
            Assert.Equal("123 Main St, Kuwait", order.ShippingAddress);
            Assert.Equal(DeliveryMethod.Courier, order.DeliveryMethod);
        }

        [Fact]
        public async Task CreateAsync_ReturnsSuccess_WhenDeliveryIsPickupWithNoShippingAddress()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _currentTenant.UserId.Returns(userId);

            var request = new CreateCardOrderRequest 
            { 
                Quantity = 5, 
                DeliveryMethod = DeliveryMethod.Pickup, 
                ShippingAddress = null 
            };
            var order = new CardOrder 
            { 
                Id = Guid.NewGuid(), 
                Quantity = 5, 
                DeliveryMethod = DeliveryMethod.Pickup, 
                ShippingAddress = null, 
                Items = [] 
            };
            _mapper.Map<CardOrder>(request).Returns(order);
            _orderRepo.GetQueryable().Returns(new List<CardOrder> { order }.AsQueryable().BuildMock());
            _mapper.Map<CardOrderDto>(order).Returns(new CardOrderDto());

            // Act
            var result = await _sut.CreateAsync(request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(200, result.StatusCode);
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
                AssignmentScope = "individual",
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
            var result = await _sut.DeleteAsync(id);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task DeleteAsync_ReturnsSuccess_AndRemovesOrder()
        {
            // Arrange
            var id = Guid.NewGuid();
            var order = new CardOrder { Id = id };
            _orderRepo.GetByIdAsync(id).Returns(order);
            _messageService.Get("RecordDeleted").Returns("Record deleted.");

            // Act
            var result = await _sut.DeleteAsync(id);

            // Assert
            Assert.True(result.IsSuccess);
            _orderRepo.Received(1).Remove(order);
            await _unitOfWork.Received(1).SaveChangesAsync();
        }

        // ── Bulk Ingestion / Background Jobs ──────────────────────────────────────

        [Fact]
        public async Task QueueEmployeeImportJobAsync_ReturnsUnauthorized_WhenUserNotAuthenticated()
        {
            // Arrange
            _currentTenant.TenantId.Returns((Guid?)null);
            var file = Substitute.For<Microsoft.AspNetCore.Http.IFormFile>();

            // Act
            var result = await _sut.QueueEmployeeImportJobAsync(file, CardType.Plastic, CardDesignType.BuiltInTemplate, null);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(401, result.StatusCode);
        }

        [Fact]
        public async Task QueueEmployeeImportJobAsync_ReturnsBadRequest_WhenFileNullOrEmpty()
        {
            // Arrange
            _currentTenant.TenantId.Returns(Guid.NewGuid());
            _currentTenant.UserId.Returns(Guid.NewGuid());

            // Act
            var result = await _sut.QueueEmployeeImportJobAsync(null!, CardType.Plastic, CardDesignType.BuiltInTemplate, null);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(400, result.StatusCode);
            Assert.Contains("NoFileUploaded", result.Message);
        }

        [Fact]
        public async Task QueueEmployeeImportJobAsync_ReturnsBadRequest_WhenExtensionInvalid()
        {
            // Arrange
            _currentTenant.TenantId.Returns(Guid.NewGuid());
            _currentTenant.UserId.Returns(Guid.NewGuid());

            var file = Substitute.For<Microsoft.AspNetCore.Http.IFormFile>();
            file.FileName.Returns("test.txt");
            file.Length.Returns(100);

            // Act
            var result = await _sut.QueueEmployeeImportJobAsync(file, CardType.Plastic, CardDesignType.BuiltInTemplate, null);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(400, result.StatusCode);
            Assert.Contains("ExcelFilesOnly", result.Message);
        }

        [Fact]
        public async Task QueueEmployeeImportJobAsync_ReturnsSuccess_QueuesJobAndEnqueuesHangfire()
        {
            // Arrange
            _currentTenant.TenantId.Returns(Guid.NewGuid());
            _currentTenant.UserId.Returns(Guid.NewGuid());

            var file = Substitute.For<Microsoft.AspNetCore.Http.IFormFile>();
            file.FileName.Returns("import.xlsx");
            file.Length.Returns(100);

            var uploadResult = new NFC.Platform.Application.DTOs.Upload.UploadResultDto
            {
                SecureUrl = "https://cloudinary.com/raw/import.xlsx",
                PublicId = "cloudinary-id"
            };
            _storageService.UploadRawFileAsync(file, "employee-imports").Returns(Task.FromResult(uploadResult));
            _messageService.Get("RecordCreated").Returns("Success message");

            // Act
            var result = await _sut.QueueEmployeeImportJobAsync(file, CardType.Plastic, CardDesignType.BuiltInTemplate, "Notes test");

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal("import.xlsx", result.Data.FileName);
            Assert.Equal(EmployeeImportJobStatus.Pending, result.Data.Status);
            await _jobRepo.Received(1).AddAsync(Arg.Any<EmployeeImportJob>());
            await _unitOfWork.Received(1).SaveChangesAsync();
            _backgroundJobClient.Received(1).Create(
                Arg.Is<Hangfire.Common.Job>(j => j.Method.Name == "ProcessEmployeeImportJobAsync"),
                Arg.Any<Hangfire.States.IState>());
        }

        [Fact]
        public async Task ProcessEmployeeImportJobAsync_ExitsEarly_WhenJobNotFound()
        {
            // Arrange
            var jobId = Guid.NewGuid();
            var jobQueryable = new List<EmployeeImportJob>().BuildMock();
            _jobRepo.GetQueryable().Returns(jobQueryable);

            // Act
            await _sut.ProcessEmployeeImportJobAsync(jobId);

            // Assert
            await _unitOfWork.DidNotReceive().SaveChangesAsync();
        }

        [Fact]
        public async Task ProcessEmployeeImportJobAsync_ExitsEarly_WhenJobNotPending()
        {
            // Arrange
            var jobId = Guid.NewGuid();
            var job = new EmployeeImportJob { Id = jobId, Status = EmployeeImportJobStatus.Processing };
            var jobQueryable = new List<EmployeeImportJob> { job }.BuildMock();
            _jobRepo.GetQueryable().Returns(jobQueryable);

            // Act
            await _sut.ProcessEmployeeImportJobAsync(jobId);

            // Assert
            await _unitOfWork.DidNotReceive().SaveChangesAsync();
        }

        [Fact]
        public async Task ProcessEmployeeImportJobAsync_FailsJob_WhenDownloadThrows()
        {
            // Arrange
            var jobId = Guid.NewGuid();
            var job = new EmployeeImportJob
            {
                Id = jobId,
                Status = EmployeeImportJobStatus.Pending,
                ExcelFileUrl = "invalid-url-format"
            };
            var jobQueryable = new List<EmployeeImportJob> { job }.BuildMock();
            _jobRepo.GetQueryable().Returns(jobQueryable);

            // Act
            await _sut.ProcessEmployeeImportJobAsync(jobId);

            // Assert
            Assert.Equal(EmployeeImportJobStatus.Failed, job.Status);
            Assert.Contains("FailedToDownloadExcel", job.ErrorsJson);
            await _unitOfWork.Received(2).SaveChangesAsync();
        }

        [Fact]
        public async Task ProcessEmployeeImportJobAsync_FailsJob_WhenExcelParserThrows()
        {
            // Arrange
            using var server = new TestHttpServer(new byte[] { 1, 2, 3 });
            var jobId = Guid.NewGuid();
            var job = new EmployeeImportJob
            {
                Id = jobId,
                Status = EmployeeImportJobStatus.Pending,
                ExcelFileUrl = server.Url + "import.xlsx",
                TenantId = Guid.NewGuid()
            };

            var jobQueryable = new List<EmployeeImportJob> { job }.BuildMock();
            _jobRepo.GetQueryable().Returns(jobQueryable);

            _excelParser.ParseEmployeesFromExcel(Arg.Any<Stream>()).Returns(_ => throw new Exception("Parser corrupt"));

            // Act
            await _sut.ProcessEmployeeImportJobAsync(jobId);

            // Assert
            Assert.Equal(EmployeeImportJobStatus.Failed, job.Status);
            Assert.Contains("FailedToParseExcel", job.ErrorsJson);
        }

        [Fact]
        public async Task ProcessEmployeeImportJobAsync_FailsJob_WhenExcelIsEmpty()
        {
            // Arrange
            using var server = new TestHttpServer(new byte[] { 1, 2, 3 });
            var jobId = Guid.NewGuid();
            var job = new EmployeeImportJob
            {
                Id = jobId,
                Status = EmployeeImportJobStatus.Pending,
                ExcelFileUrl = server.Url + "import.xlsx",
                TenantId = Guid.NewGuid()
            };

            var jobQueryable = new List<EmployeeImportJob> { job }.BuildMock();
            _jobRepo.GetQueryable().Returns(jobQueryable);

            _excelParser.ParseEmployeesFromExcel(Arg.Any<Stream>()).Returns(new List<ExcelEmployeeImportDto>());

            // Act
            await _sut.ProcessEmployeeImportJobAsync(jobId);

            // Assert
            Assert.Equal(EmployeeImportJobStatus.Failed, job.Status);
            Assert.Contains("NoValidEmployeeRows", job.ErrorsJson);
        }

        [Fact]
        public async Task ProcessEmployeeImportJobAsync_FailsJob_WhenRowValidationFails()
        {
            // Arrange
            using var server = new TestHttpServer(new byte[] { 1, 2, 3 });
            var jobId = Guid.NewGuid();
            var job = new EmployeeImportJob
            {
                Id = jobId,
                Status = EmployeeImportJobStatus.Pending,
                ExcelFileUrl = server.Url + "import.xlsx",
                TenantId = Guid.NewGuid()
            };

            var jobQueryable = new List<EmployeeImportJob> { job }.BuildMock();
            _jobRepo.GetQueryable().Returns(jobQueryable);

            var rows = new List<ExcelEmployeeImportDto>
            {
                new ExcelEmployeeImportDto { Name = "", Email = "invalid-email" }
            };
            _excelParser.ParseEmployeesFromExcel(Arg.Any<Stream>()).Returns(rows);

            // Act
            await _sut.ProcessEmployeeImportJobAsync(jobId);

            // Assert
            Assert.Equal(EmployeeImportJobStatus.Failed, job.Status);
            Assert.Contains("ImportRowNameRequired", job.ErrorsJson);
            Assert.Contains("ImportRowEmailInvalid", job.ErrorsJson);
        }

        [Fact]
        public async Task ProcessEmployeeImportJobAsync_FailsJob_WhenCompanyNotFound()
        {
            // Arrange
            using var server = new TestHttpServer(new byte[] { 1, 2, 3 });
            var jobId = Guid.NewGuid();
            var job = new EmployeeImportJob
            {
                Id = jobId,
                Status = EmployeeImportJobStatus.Pending,
                ExcelFileUrl = server.Url + "import.xlsx",
                TenantId = Guid.NewGuid()
            };

            var jobQueryable = new List<EmployeeImportJob> { job }.BuildMock();
            _jobRepo.GetQueryable().Returns(jobQueryable);

            var rows = new List<ExcelEmployeeImportDto>
            {
                new ExcelEmployeeImportDto { Name = "John Doe", Email = "john@example.com" }
            };
            _excelParser.ParseEmployeesFromExcel(Arg.Any<Stream>()).Returns(rows);

            var companyRepo = Substitute.For<IGenericRepository<Company>>();
            companyRepo.GetQueryable().Returns(new List<Company>().BuildMock());
            _unitOfWork.Repository<Company>().Returns(companyRepo);

            // Act
            await _sut.ProcessEmployeeImportJobAsync(jobId);

            // Assert
            Assert.Equal(EmployeeImportJobStatus.Failed, job.Status);
            Assert.Contains("CompanyNotFound", job.ErrorsJson);
        }

        [Fact]
        public async Task ProcessEmployeeImportJobAsync_FailsJob_WhenSubscriptionExpiredOrMissing()
        {
            // Arrange
            using var server = new TestHttpServer(new byte[] { 1, 2, 3 });
            var jobId = Guid.NewGuid();
            var job = new EmployeeImportJob
            {
                Id = jobId,
                Status = EmployeeImportJobStatus.Pending,
                ExcelFileUrl = server.Url + "import.xlsx",
                TenantId = Guid.NewGuid()
            };

            var jobQueryable = new List<EmployeeImportJob> { job }.BuildMock();
            _jobRepo.GetQueryable().Returns(jobQueryable);

            var rows = new List<ExcelEmployeeImportDto>
            {
                new ExcelEmployeeImportDto { Name = "John Doe", Email = "john@example.com" }
            };
            _excelParser.ParseEmployeesFromExcel(Arg.Any<Stream>()).Returns(rows);

            var companyRepo = Substitute.For<IGenericRepository<Company>>();
            companyRepo.GetQueryable().Returns(new List<Company> { new Company { TenantId = job.TenantId } }.BuildMock());
            _unitOfWork.Repository<Company>().Returns(companyRepo);

            var subRepo = Substitute.For<IGenericRepository<UserSubscription>>();
            subRepo.GetQueryable().Returns(new List<UserSubscription>().BuildMock());
            _unitOfWork.Repository<UserSubscription>().Returns(subRepo);

            // Act
            await _sut.ProcessEmployeeImportJobAsync(jobId);

            // Assert
            Assert.Equal(EmployeeImportJobStatus.Failed, job.Status);
            Assert.Contains("SubscriptionExpiredOrMissing", job.ErrorsJson);
        }

        [Fact]
        public async Task ProcessEmployeeImportJobAsync_FailsJob_WhenMaxLimitReached()
        {
            // Arrange
            using var server = new TestHttpServer(new byte[] { 1, 2, 3 });
            var jobId = Guid.NewGuid();
            var job = new EmployeeImportJob
            {
                Id = jobId,
                Status = EmployeeImportJobStatus.Pending,
                ExcelFileUrl = server.Url + "import.xlsx",
                TenantId = Guid.NewGuid()
            };

            var jobQueryable = new List<EmployeeImportJob> { job }.BuildMock();
            _jobRepo.GetQueryable().Returns(jobQueryable);

            var rows = new List<ExcelEmployeeImportDto>
            {
                new ExcelEmployeeImportDto { Name = "John Doe", Email = "john@example.com" }
            };
            _excelParser.ParseEmployeesFromExcel(Arg.Any<Stream>()).Returns(rows);

            var companyRepo = Substitute.For<IGenericRepository<Company>>();
            companyRepo.GetQueryable().Returns(new List<Company> { new Company { TenantId = job.TenantId } }.BuildMock());
            _unitOfWork.Repository<Company>().Returns(companyRepo);

            var plan = new SubscriptionPlan { MaxEmployees = 1 };
            var activeSub = new UserSubscription { TenantId = job.TenantId, IsActive = true, EndDate = DateTime.UtcNow.AddDays(10), SubscriptionPlan = plan };
            var subRepo = Substitute.For<IGenericRepository<UserSubscription>>();
            subRepo.GetQueryable().Returns(new List<UserSubscription> { activeSub }.BuildMock());
            _unitOfWork.Repository<UserSubscription>().Returns(subRepo);

            var employeeRepo = Substitute.For<IGenericRepository<Employee>>();
            employeeRepo.CountAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Employee, bool>>>()).Returns(Task.FromResult(1));
            employeeRepo.GetQueryable().Returns(new List<Employee>().BuildMock());
            _unitOfWork.Repository<Employee>().Returns(employeeRepo);

            var userRepo = Substitute.For<IGenericRepository<User>>();
            userRepo.GetQueryable().Returns(new List<User>().BuildMock());
            _unitOfWork.Repository<User>().Returns(userRepo);

            // Act
            await _sut.ProcessEmployeeImportJobAsync(jobId);

            // Assert
            Assert.Equal(EmployeeImportJobStatus.Failed, job.Status);
            Assert.Contains("MaxEmployeesLimitReached", job.ErrorsJson);
        }

        [Fact]
        public async Task ProcessEmployeeImportJobAsync_Succeeds_WhenValidInputs()
        {
            // Arrange
            using var server = new TestHttpServer(new byte[] { 1, 2, 3 });
            var jobId = Guid.NewGuid();
            var job = new EmployeeImportJob
            {
                Id = jobId,
                Status = EmployeeImportJobStatus.Pending,
                ExcelFileUrl = server.Url + "import.xlsx",
                TenantId = Guid.NewGuid(),
                CardType = CardType.Plastic,
                CardDesignType = CardDesignType.BuiltInTemplate
            };

            var jobQueryable = new List<EmployeeImportJob> { job }.BuildMock();
            _jobRepo.GetQueryable().Returns(jobQueryable);

            var rows = new List<ExcelEmployeeImportDto>
            {
                new ExcelEmployeeImportDto { Name = "John Doe", Email = "john@example.com", JobTitle = "Engineer", Department = "IT" }
            };
            _excelParser.ParseEmployeesFromExcel(Arg.Any<Stream>()).Returns(rows);

            var company = new Company { Id = Guid.NewGuid(), Name = "OnPoint", TenantId = job.TenantId };
            var companyRepo = Substitute.For<IGenericRepository<Company>>();
            companyRepo.GetQueryable().Returns(new List<Company> { company }.BuildMock());
            _unitOfWork.Repository<Company>().Returns(companyRepo);

            var plan = new SubscriptionPlan { MaxEmployees = 10 };
            var activeSub = new UserSubscription { TenantId = job.TenantId, IsActive = true, EndDate = DateTime.UtcNow.AddDays(10), SubscriptionPlan = plan };
            var subRepo = Substitute.For<IGenericRepository<UserSubscription>>();
            subRepo.GetQueryable().Returns(new List<UserSubscription> { activeSub }.BuildMock());
            _unitOfWork.Repository<UserSubscription>().Returns(subRepo);

            var employeeRepo = Substitute.For<IGenericRepository<Employee>>();
            employeeRepo.CountAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Employee, bool>>>()).Returns(Task.FromResult(0));
            employeeRepo.GetQueryable().Returns(new List<Employee>().BuildMock());
            _unitOfWork.Repository<Employee>().Returns(employeeRepo);

            var userRepo = Substitute.For<IGenericRepository<User>>();
            userRepo.GetQueryable().Returns(new List<User>().BuildMock());
            _unitOfWork.Repository<User>().Returns(userRepo);

            var userProfileRepo = Substitute.For<IGenericRepository<UserProfile>>();
            _unitOfWork.Repository<UserProfile>().Returns(userProfileRepo);

            var mappedEmployee = new Employee { Id = Guid.NewGuid(), Email = "john@example.com" };
            var mappedProfile = new UserProfile { Id = Guid.NewGuid() };
            var mappedItem = new CardOrderItem { UserProfileId = mappedProfile.Id };

            _mapper.Map<Employee>(Arg.Any<ExcelEmployeeImportDto>()).Returns(mappedEmployee);
            _mapper.Map<UserProfile>(Arg.Any<ExcelEmployeeImportDto>()).Returns(mappedProfile);
            _mapper.Map<CardOrderItem>(Arg.Any<ExcelEmployeeImportDto>()).Returns(mappedItem);

            // Act
            await _sut.ProcessEmployeeImportJobAsync(jobId);

            // Assert
            Assert.Equal(EmployeeImportJobStatus.Completed, job.Status);
            Assert.Equal(1, job.Imported);
            Assert.Equal(0, job.Skipped);
            Assert.NotNull(job.CardOrderId);
            await _unitOfWork.Received(1).CommitTransactionAsync();
        }

        private class TestHttpServer : IDisposable
        {
            private readonly System.Net.HttpListener _listener;
            public string Url { get; }
            private readonly byte[] _responseBytes;

            public TestHttpServer(byte[] responseBytes)
            {
                _responseBytes = responseBytes;
                _listener = new System.Net.HttpListener();
                var port = GetFreePort();
                Url = $"http://localhost:{port}/";
                _listener.Prefixes.Add(Url);
                _listener.Start();
                _listener.BeginGetContext(OnRequest, null);
            }

            private void OnRequest(IAsyncResult ar)
            {
                try
                {
                    var context = _listener.EndGetContext(ar);
                    context.Response.ContentType = "application/octet-stream";
                    context.Response.ContentLength64 = _responseBytes.Length;
                    context.Response.OutputStream.Write(_responseBytes, 0, _responseBytes.Length);
                    context.Response.OutputStream.Close();
                    _listener.BeginGetContext(OnRequest, null);
                }
                catch
                {
                }
            }

            private static int GetFreePort()
            {
                var l = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
                l.Start();
                var port = ((System.Net.IPEndPoint)l.LocalEndpoint).Port;
                l.Stop();
                return port;
            }

            public void Dispose()
            {
                try
                {
                    _listener.Stop();
                    _listener.Close();
                }
                catch { }
            }
        }

        // ── CardPricing Database Tests ────────────────────────────────────────────

        [Fact]
        public async Task GetOrderPricingAsync_QueriesDatabase_WhenValidCardTypePassed()
        {
            // Arrange
            var pricing = new CardPricing
            {
                CardType = CardType.Metal,
                UnitPrice = 8.5m,
                Currency = "KWD",
                IsActive = true,
                EffectiveFrom = DateTime.UtcNow.AddDays(-1)
            };
            var pricingsList = new List<CardPricing> { pricing };
            var mockQueryable = pricingsList.AsQueryable().BuildMock();
            _cardPricingRepo.GetQueryable().Returns(mockQueryable);

            // Act
            var result = await _sut.GetOrderPricingAsync("metal", 5);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(8.5m, result.Data!.UnitPrice);
            Assert.Equal(42.5m, result.Data!.TotalPrice);
            Assert.Equal("KWD", result.Data!.Currency);
        }

        [Fact]
        public async Task GetOrderPricingAsync_ReturnsFailure_WhenInvalidCardTypePassed()
        {
            // Act
            var result = await _sut.GetOrderPricingAsync("invalid_material", 5);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public async Task CreateAsync_SavesPricingSnapshotOnOrder()
        {
            // Arrange
            var pricing = new CardPricing
            {
                CardType = CardType.Plastic,
                UnitPrice = 4.5m,
                Currency = "KWD",
                IsActive = true,
                EffectiveFrom = DateTime.UtcNow.AddDays(-1)
            };
            var pricingsList = new List<CardPricing> { pricing };
            var mockQueryable = pricingsList.AsQueryable().BuildMock();
            _cardPricingRepo.GetQueryable().Returns(mockQueryable);

            _currentTenant.UserId.Returns(Guid.NewGuid());

            var request = new CreateCardOrderRequest
            {
                CardType = CardType.Plastic,
                Quantity = 10,
                CardName = "Test Plastic Order"
            };

            var mappedOrder = new CardOrder
            {
                CardType = CardType.Plastic,
                Quantity = 10,
                CardName = "Test Plastic Order"
            };

            _mapper.Map<CardOrder>(request).Returns(mappedOrder);
            _mapper.Map<CardOrderDto>(Arg.Any<CardOrder>()).Returns(new CardOrderDto());

            var savedOrders = new List<CardOrder>();
            _orderRepo.AddAsync(Arg.Do<CardOrder>(o => savedOrders.Add(o))).Returns(Task.CompletedTask);

            // Set up get queryable for reloading
            var mockOrderQueryable = savedOrders.AsQueryable().BuildMock();
            _orderRepo.GetQueryable().Returns(mockOrderQueryable);

            // Act
            var result = await _sut.CreateAsync(request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Single(savedOrders);
            var savedOrder = savedOrders[0];
            Assert.Equal(4.5m, savedOrder.UnitPrice);
            Assert.Equal("KWD", savedOrder.Currency);
            Assert.Equal(45.0m, savedOrder.TotalPrice);
        }

        [Fact]
        public async Task GetOrderPricingAsync_ReturnsPricing_ForValidCardTypes()
        {
            // Act
            var resultPlastic = await _sut.GetOrderPricingAsync("plastic", 10);
            var resultMetal = await _sut.GetOrderPricingAsync("metal", 5);

            // Assert
            Assert.True(resultPlastic.IsSuccess);
            Assert.Equal(45.0m, resultPlastic.Data!.TotalPrice);
            Assert.True(resultMetal.IsSuccess);
            Assert.Equal(42.5m, resultMetal.Data!.TotalPrice);
        }

        [Fact]
        public async Task GetOrderPricingAsync_Returns400_ForInvalidCardType()
        {
            // Act
            var result = await _sut.GetOrderPricingAsync("gold_plated_diamond", 10);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public async Task GetOrderPricingAsync_Returns500_WhenPricingNotConfigured()
        {
            // Arrange
            _cardPricingRepo.GetQueryable().Returns(new List<CardPricing>().AsQueryable().BuildMock());

            // Act
            var result = await _sut.GetOrderPricingAsync("plastic", 10);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(500, result.StatusCode);
        }

        [Fact]
        public async Task CreateReorderAsync_ReturnsUnauthorized_WhenUserIdIsNull()
        {
            // Arrange
            _currentTenant.UserId.Returns((Guid?)null);

            // Act
            var result = await _sut.CreateReorderAsync(Guid.NewGuid(), new ReorderRequest { AssignmentScope = "individual" });

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
            var result = await _sut.CreateReorderAsync(Guid.NewGuid(), new ReorderRequest { AssignmentScope = "individual" });

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
                AssignmentScope = "specific_employees",
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
        public async Task CreateReorderAsync_Returns500_WhenPricingNotConfigured()
        {
            // Arrange
            var parentId = Guid.NewGuid();
            _currentTenant.UserId.Returns(Guid.NewGuid());
            var parentOrder = new CardOrder { Id = parentId, CardType = CardType.Plastic };
            _orderRepo.GetQueryable().Returns(new List<CardOrder> { parentOrder }.AsQueryable().BuildMock());
            
            // Empty pricing
            _cardPricingRepo.GetQueryable().Returns(new List<CardPricing>().AsQueryable().BuildMock());

            var request = new ReorderRequest { Quantity = 5, AssignmentScope = "individual" };

            // Act
            var result = await _sut.CreateReorderAsync(parentId, request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(500, result.StatusCode);
        }

        [Fact]
        public async Task CreateReorderAsync_ReturnsSuccess_WhenReorderIsValid()
        {
            // Arrange
            var parentId = Guid.NewGuid();
            _currentTenant.UserId.Returns(Guid.NewGuid());
            var parentOrder = new CardOrder { Id = parentId, CardType = CardType.Plastic, CardName = "Parent Card" };
            _orderRepo.GetQueryable().Returns(new List<CardOrder> { parentOrder }.AsQueryable().BuildMock());

            var request = new ReorderRequest { Quantity = 5, AssignmentScope = "individual" };

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
                AssignmentScope = "specific_employees",
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
                AssignmentScope = "specific_employees",
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
                AssignmentScope = "specific_employees",
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
                AssignmentScope = "all_employees",
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
        public async Task CreateReorderAsync_Returns422_WhenAllEmployeesQuantityMismatch()
        {
            // Arrange
            var parentId = Guid.NewGuid();
            _currentTenant.UserId.Returns(Guid.NewGuid());
            var parentOrder = new CardOrder { Id = parentId, CardType = CardType.Plastic };
            _orderRepo.GetQueryable().Returns(new List<CardOrder> { parentOrder }.AsQueryable().BuildMock());

            var request = new ReorderRequest
            {
                AssignmentScope = "all_employees",
                Quantity = 5
            };

            var employees = new List<Employee>
            {
                new Employee { Id = Guid.NewGuid(), FullName = "Emp 1", IsDeleted = false, UserProfile = new UserProfile() }
            };

            var employeeRepo = Substitute.For<IGenericRepository<Employee>>();
            employeeRepo.GetQueryable().Returns(employees.AsQueryable().BuildMock());
            _unitOfWork.Repository<Employee>().Returns(employeeRepo);

            // Act
            var result = await _sut.CreateReorderAsync(parentId, request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(422, result.StatusCode);
            Assert.Contains("EmployeeCountMismatch", result.Message);
        }

        [Fact]
        public async Task ReissueCardAsync_Returns404_WhenCardDoesNotExist()
        {
            // Arrange
            _currentTenant.UserId.Returns(Guid.NewGuid());
            _cardRepo.GetQueryable().Returns(new List<Card>().AsQueryable().BuildMock());

            // Act
            var result = await _sut.ReissueCardAsync(Guid.NewGuid(), new ReissueCardRequest());

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task ReissueCardAsync_Returns422_WhenCardHasNoUserProfile()
        {
            // Arrange
            var cardId = Guid.NewGuid();
            _currentTenant.UserId.Returns(Guid.NewGuid());
            var card = new Card { Id = cardId, UserProfileId = null, UserProfile = null };
            _cardRepo.GetQueryable().Returns(new List<Card> { card }.AsQueryable().BuildMock());

            // Act
            var result = await _sut.ReissueCardAsync(cardId, new ReissueCardRequest());

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(422, result.StatusCode);
        }

        [Fact]
        public async Task ReissueCardAsync_Returns422_WhenCourierMissingAddress()
        {
            // Arrange
            _currentTenant.UserId.Returns(Guid.NewGuid());
            var request = new ReissueCardRequest { DeliveryMethod = DeliveryMethod.Courier, ShippingAddress = null };

            // Act
            var result = await _sut.ReissueCardAsync(Guid.NewGuid(), request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(422, result.StatusCode);
        }

        [Fact]
        public async Task ReissueCardAsync_DeactivatesOldCard_AndCreatesNewOrderAndItem_OnSuccess()
        {
            // Arrange
            var cardId = Guid.NewGuid();
            var tenantId = Guid.NewGuid();
            var profileId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            _currentTenant.UserId.Returns(userId);

            var userProfile = new UserProfile
            {
                Id = profileId,
                FullName = "Emp 1",
                JobTitle = "Title 1",
                Department = "Dept 1",
                Phone = "123456",
                ContactEmail = "emp1@test.com"
            };

            var originalOrder = new CardOrder
            {
                Id = Guid.NewGuid(),
                CardType = CardType.Plastic,
                CardName = "Original Order Name",
                CardDesignType = CardDesignType.BuiltInTemplate
            };

            var card = new Card
            {
                Id = cardId,
                TenantId = tenantId,
                UserProfileId = profileId,
                UserProfile = userProfile,
                CardOrder = originalOrder,
                Status = CardStatus.Active
            };

            _cardRepo.GetQueryable().Returns(new List<Card> { card }.AsQueryable().BuildMock());

            var request = new ReissueCardRequest
            {
                DeliveryMethod = DeliveryMethod.Courier,
                ShippingAddress = "Replacement Address"
            };

            // Set up order queryable for the reload inside ReissueCardAsync
            var createdOrder = new CardOrder { Id = Guid.NewGuid(), Items = [] };
            _orderRepo.GetQueryable().Returns(new List<CardOrder> { createdOrder }.AsQueryable().BuildMock());
            _mapper.Map<CardOrderDto>(createdOrder).Returns(new CardOrderDto { Id = createdOrder.Id });

            var mappedItem = new CardOrderItem
            {
                UserProfileId = profileId,
                EmployeeName = "Emp 1",
                Phone = "123456",
                Email = "emp1@test.com"
            };
            _mapper.Map<CardOrderItem>(userProfile).Returns(mappedItem);

            // Act
            var result = await _sut.ReissueCardAsync(cardId, request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(CardStatus.Deactivated, card.Status); // Old card must be deactivated
            await _orderRepo.Received(1).AddAsync(Arg.Is<CardOrder>(o =>
                o.UserId == userId &&
                o.Quantity == 1 &&
                o.DeliveryMethod == DeliveryMethod.Courier &&
                o.ShippingAddress == "Replacement Address" &&
                o.Items.Count == 1 &&
                o.Items.First().UserProfileId == profileId &&
                o.Items.First().EmployeeName == "Emp 1" &&
                o.Items.First().Phone == "123456" &&
                o.Items.First().Email == "emp1@test.com"
            ));
            await _unitOfWork.Received(1).SaveChangesAsync();
        }

        [Fact]
        public async Task GetEmployeesImportStatusAsync_ReturnsJobStatus_WhenJobExists()
        {
            // Arrange
            var jobId = Guid.NewGuid();
            var job = new EmployeeImportJob
            {
                Id = jobId,
                Status = EmployeeImportJobStatus.Completed,
                TotalRows = 10,
                Imported = 8,
                Skipped = 2
            };
            _jobRepo.GetQueryable().Returns(new List<EmployeeImportJob> { job }.AsQueryable().BuildMock());
            _mapper.Map<EmployeesImportStatusDto>(job).Returns(new EmployeesImportStatusDto
            {
                Status = job.Status.ToString(),
                TotalRows = job.TotalRows,
                Imported = job.Imported,
                Skipped = job.Skipped,
                Errors = new List<string>()
            });

            // Act
            var result = await _sut.GetEmployeesImportStatusAsync(jobId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("Completed", result.Data!.Status);
            Assert.Equal(10, result.Data.TotalRows);
            Assert.Equal(8, result.Data.Imported);
            Assert.Equal(2, result.Data.Skipped);
        }

        [Fact]
        public async Task GetEmployeesImportStatusAsync_FallsBackToCardOrder_WhenJobDoesNotExist()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            _jobRepo.GetQueryable().Returns(new List<EmployeeImportJob>().AsQueryable().BuildMock());

            var order = new CardOrder { Id = orderId, Quantity = 5, Items = new List<CardOrderItem> { new CardOrderItem(), new CardOrderItem() } };
            _orderRepo.GetQueryable().Returns(new List<CardOrder> { order }.AsQueryable().BuildMock());

            var expectedDto = new EmployeesImportStatusDto { TotalRows = 5 };
            _mapper.Map<EmployeesImportStatusDto>(order).Returns(expectedDto);

            // Act
            var result = await _sut.GetEmployeesImportStatusAsync(orderId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("Completed", result.Data!.Status);
            Assert.Equal(5, result.Data.TotalRows);
        }

        [Fact]
        public async Task GetEmployeesImportStatusAsync_ReturnsNotFound_WhenNeitherExists()
        {
            // Arrange
            var id = Guid.NewGuid();
            _jobRepo.GetQueryable().Returns(new List<EmployeeImportJob>().AsQueryable().BuildMock());
            _orderRepo.GetQueryable().Returns(new List<CardOrder>().AsQueryable().BuildMock());

            // Act
            var result = await _sut.GetEmployeesImportStatusAsync(id);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task GetActivePricingCatalogAsync_ReturnsOnlyActivePricing()
        {
            // Arrange
            var pricings = new List<CardPricing>
            {
                new CardPricing { IsActive = true, EffectiveFrom = DateTime.UtcNow.AddDays(-1), EffectiveTo = null },
                new CardPricing { IsActive = false, EffectiveFrom = DateTime.UtcNow.AddDays(-5), EffectiveTo = DateTime.UtcNow.AddDays(-1) }
            };
            _cardPricingRepo.GetQueryable().Returns(pricings.AsQueryable().BuildMock());

            var dtos = new List<CardPricingDto> { new CardPricingDto() };
            _mapper.Map<IReadOnlyList<CardPricingDto>>(Arg.Is<List<CardPricing>>(l => l.Count == 1)).Returns(dtos);

            // Act
            var result = await _sut.GetActivePricingCatalogAsync();

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Single(result.Data!);
        }
    }
}
