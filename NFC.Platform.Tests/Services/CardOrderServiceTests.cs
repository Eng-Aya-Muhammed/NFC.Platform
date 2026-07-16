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

            _unitOfWork.Repository<CardOrder>().Returns(_orderRepo);
            _unitOfWork.Repository<Card>().Returns(_cardRepo);
            _unitOfWork.Repository<CardOrderItem>().Returns(_orderItemRepo);
            _unitOfWork.Repository<EmployeeImportJob>().Returns(_jobRepo);

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
            var result = await _sut.QueueEmployeeImportJobAsync(file, CardType.Plastic, CardDesignType.BuiltInTemplate, null, null);

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
            var result = await _sut.QueueEmployeeImportJobAsync(null!, CardType.Plastic, CardDesignType.BuiltInTemplate, null, null);

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
            var result = await _sut.QueueEmployeeImportJobAsync(file, CardType.Plastic, CardDesignType.BuiltInTemplate, null, null);

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
            var result = await _sut.QueueEmployeeImportJobAsync(file, CardType.Plastic, CardDesignType.BuiltInTemplate, null, "Notes test");

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
    }
}
