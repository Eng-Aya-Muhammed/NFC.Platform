namespace NFC.Platform.Tests.Services
{
    public class EmployeeImportServiceTests
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IMessageService _messageService;
        private readonly ICurrentTenant _currentTenant;
        private readonly IExcelParser _excelParser;
        private readonly System.Net.Http.IHttpClientFactory _httpClientFactory;

        private readonly IGenericRepository<Employee> _employeeRepo;
        private readonly IGenericRepository<UserProfile> _userProfileRepo;
        private readonly IGenericRepository<UserSubscription> _subscriptionRepo;
        private readonly IGenericRepository<Company> _companyRepo;

        private readonly EmployeeService _sut;
        private readonly Guid _tenantId = Guid.NewGuid();
        private readonly Guid _companyId = Guid.NewGuid();

        public EmployeeImportServiceTests()
        {
            _unitOfWork    = Substitute.For<IUnitOfWork>();
            _mapper        = Substitute.For<IMapper>();
            _messageService = Substitute.For<IMessageService>();
            _currentTenant = Substitute.For<ICurrentTenant>();
            _excelParser   = Substitute.For<IExcelParser>();
            _httpClientFactory = Substitute.For<System.Net.Http.IHttpClientFactory>();

            _employeeRepo    = Substitute.For<IGenericRepository<Employee>>();
            _userProfileRepo = Substitute.For<IGenericRepository<UserProfile>>();
            _subscriptionRepo = Substitute.For<IGenericRepository<UserSubscription>>();
            _companyRepo     = Substitute.For<IGenericRepository<Company>>();

            _unitOfWork.Repository<Employee>().Returns(_employeeRepo);
            _unitOfWork.Repository<UserProfile>().Returns(_userProfileRepo);
            _unitOfWork.Repository<UserSubscription>().Returns(_subscriptionRepo);
            _unitOfWork.Repository<Company>().Returns(_companyRepo);

            _messageService.Get(Arg.Any<string>()).Returns(x => (string)x[0]);
            _messageService.Get(Arg.Any<string>(), Arg.Any<object[]>()).Returns(x => (string)x[0]);

            // Default: no active subscription, 0 employees, empty company
            SetupSubscription(null);
            SetupEmployees();
            SetupCompany(null);
            _employeeRepo.CountAsync(Arg.Any<Expression<Func<Employee, bool>>>()).Returns(0);

            _sut = new EmployeeService(
                _unitOfWork, _mapper, _messageService, _currentTenant,
                _excelParser, _httpClientFactory);
        }

        // ─── Setup helpers ───────────────────────────────────────────────────

        private static ExcelEmployeeImportDto ValidRow(string email = "emp@test.com", string name = "Test Employee") =>
            new() { Email = email, Name = name, JobTitle = "Engineer", Department = "IT" };

        private static System.Net.Http.HttpClient FakeClient(byte[]? bytes = null) =>
            new(new FakeHttpMessageHandler(bytes ?? new byte[] { 1, 2, 3 }));

        private void SetupSubscription(UserSubscription? sub)
        {
            var list = sub is null ? new List<UserSubscription>() : new List<UserSubscription> { sub };
            _subscriptionRepo.GetQueryable().Returns(list.AsQueryable().BuildMock());
        }

        private void SetupEmployees(List<Employee>? list = null)
        {
            _employeeRepo.GetQueryable().Returns((list ?? new List<Employee>()).AsQueryable().BuildMock());
        }

        private void SetupCompany(Company? company)
        {
            var list = company is null ? new List<Company>() : new List<Company> { company };
            _companyRepo.GetQueryable().Returns(list.AsQueryable().BuildMock());
        }

        private UserSubscription ActiveSub() => new()
        {
            TenantId = _tenantId,
            IsActive = true,
            EndDate  = DateTime.UtcNow.AddDays(30),
            SubscriptionPlan = new SubscriptionPlan()
        };

        // ─── Tests ──────────────────────────────────────────────────────────

        [Fact]
        public async Task UpsertEmployeesFromExcelAsync_ReturnsFail_WhenExcelIsEmpty()
        {
            _httpClientFactory.CreateClient().Returns(FakeClient());
            _excelParser.ParseEmployeesFromExcel(Arg.Any<Stream>())
                .Returns(new List<ExcelEmployeeImportDto>());

            var result = await _sut.UpsertEmployeesFromExcelAsync(
                "https://example.com/file.xlsx", _companyId, _tenantId);

            Assert.False(result.IsSuccess);
            Assert.Equal(422, result.StatusCode);
        }

        [Fact]
        public async Task UpsertEmployeesFromExcelAsync_ReturnsFail_WhenRowMissingName()
        {
            _httpClientFactory.CreateClient().Returns(FakeClient());
            _excelParser.ParseEmployeesFromExcel(Arg.Any<Stream>())
                .Returns(new List<ExcelEmployeeImportDto>
                {
                    new() { Email = "emp@test.com", Name = "" }
                });

            var result = await _sut.UpsertEmployeesFromExcelAsync(
                "https://example.com/file.xlsx", _companyId, _tenantId);

            Assert.False(result.IsSuccess);
            Assert.Equal(422, result.StatusCode);
        }

        [Fact]
        public async Task UpsertEmployeesFromExcelAsync_ReturnsFail_WhenRowHasInvalidEmail()
        {
            _httpClientFactory.CreateClient().Returns(FakeClient());
            _excelParser.ParseEmployeesFromExcel(Arg.Any<Stream>())
                .Returns(new List<ExcelEmployeeImportDto>
                {
                    new() { Email = "not-an-email", Name = "Test" }
                });

            var result = await _sut.UpsertEmployeesFromExcelAsync(
                "https://example.com/file.xlsx", _companyId, _tenantId);

            Assert.False(result.IsSuccess);
            Assert.Equal(422, result.StatusCode);
        }

        [Fact]
        public async Task UpsertEmployeesFromExcelAsync_ReturnsFail_WhenDuplicateEmailsInFile()
        {
            _httpClientFactory.CreateClient().Returns(FakeClient());
            _excelParser.ParseEmployeesFromExcel(Arg.Any<Stream>())
                .Returns(new List<ExcelEmployeeImportDto>
                {
                    ValidRow("dup@test.com", "Employee A"),
                    ValidRow("dup@test.com", "Employee B")
                });

            var result = await _sut.UpsertEmployeesFromExcelAsync(
                "https://example.com/file.xlsx", _companyId, _tenantId);

            Assert.False(result.IsSuccess);
            Assert.Equal(422, result.StatusCode);
        }

        [Fact]
        public async Task UpsertEmployeesFromExcelAsync_ReturnsFail_WhenNoActiveSubscription()
        {
            _httpClientFactory.CreateClient().Returns(FakeClient());
            _excelParser.ParseEmployeesFromExcel(Arg.Any<Stream>())
                .Returns(new List<ExcelEmployeeImportDto> { ValidRow() });

            // Default setup already has no subscription, no employees, no company

            var result = await _sut.UpsertEmployeesFromExcelAsync(
                "https://example.com/file.xlsx", _companyId, _tenantId);

            Assert.False(result.IsSuccess);
            Assert.Equal(422, result.StatusCode);
        }

        [Fact]
        public async Task UpsertEmployeesFromExcelAsync_UpdatesExistingEmployee_WhenEmailMatches()
        {
            _httpClientFactory.CreateClient().Returns(FakeClient());

            var existingEmployee = new Employee
            {
                Id        = Guid.NewGuid(),
                Email     = "emp@test.com",
                FullName  = "Old Name",
                TenantId  = _tenantId,
                UserProfile = new UserProfile { FullName = "Old Name" }
            };

            _excelParser.ParseEmployeesFromExcel(Arg.Any<Stream>())
                .Returns(new List<ExcelEmployeeImportDto>
                {
                    ValidRow("emp@test.com", "New Name")
                });

            SetupSubscription(ActiveSub());
            SetupEmployees(new List<Employee> { existingEmployee });
            SetupCompany(new Company { Id = _companyId, Name = "Test Co" });
            _employeeRepo.CountAsync(Arg.Any<Expression<Func<Employee, bool>>>()).Returns(1);

            var result = await _sut.UpsertEmployeesFromExcelAsync(
                "https://example.com/file.xlsx", _companyId, _tenantId);

            Assert.True(result.IsSuccess);
            Assert.Contains(existingEmployee.Id, result.Data!);
            Assert.Equal("New Name", existingEmployee.FullName);
        }
    }

    internal sealed class FakeHttpMessageHandler(byte[] content) : System.Net.Http.HttpMessageHandler
    {
        protected override Task<System.Net.Http.HttpResponseMessage> SendAsync(
            System.Net.Http.HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new System.Net.Http.ByteArrayContent(content)
            };
            return Task.FromResult(response);
        }
    }
}
