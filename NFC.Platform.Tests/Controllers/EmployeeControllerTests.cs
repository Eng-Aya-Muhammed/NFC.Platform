namespace NFC.Platform.Tests.Controllers
{
    public class EmployeeControllerTests
    {
        private readonly IEmployeeService _employeeService;
        private readonly EmployeeController _sut;

        public EmployeeControllerTests()
        {
            _employeeService = Substitute.For<IEmployeeService>();
            _sut = new EmployeeController(_employeeService);
        }

        [Fact]
        public void EmployeeController_ShouldHaveApiControllerAttribute()
        {
            var type = typeof(EmployeeController);
            var attributes = type.GetCustomAttributes(typeof(ApiControllerAttribute), true);
            Assert.NotEmpty(attributes);
        }

        [Fact]
        public void EmployeeController_Methods_ShouldHaveHasPermissionAttribute()
        {
            var type = typeof(EmployeeController);

            var getPagedMethod = type.GetMethod(nameof(EmployeeController.GetPaged));
            var getPagedAttr = getPagedMethod?.GetCustomAttributes(typeof(HasPermissionAttribute), false).FirstOrDefault() as HasPermissionAttribute;
            Assert.NotNull(getPagedAttr);
            Assert.Equal($"Permission:{AppPermissions.Employees.View}", getPagedAttr.Policy);

            var getByIdMethod = type.GetMethod(nameof(EmployeeController.GetById));
            var getByIdAttr = getByIdMethod?.GetCustomAttributes(typeof(HasPermissionAttribute), false).FirstOrDefault() as HasPermissionAttribute;
            Assert.NotNull(getByIdAttr);
            Assert.Equal($"Permission:{AppPermissions.Employees.View}", getByIdAttr.Policy);

            var createMethod = type.GetMethod(nameof(EmployeeController.Create));
            var createAttr = createMethod?.GetCustomAttributes(typeof(HasPermissionAttribute), false).FirstOrDefault() as HasPermissionAttribute;
            Assert.NotNull(createAttr);
            Assert.Equal($"Permission:{AppPermissions.Employees.Create}", createAttr.Policy);

            var updateMethod = type.GetMethod(nameof(EmployeeController.Update));
            var updateAttr = updateMethod?.GetCustomAttributes(typeof(HasPermissionAttribute), false).FirstOrDefault() as HasPermissionAttribute;
            Assert.NotNull(updateAttr);
            Assert.Equal($"Permission:{AppPermissions.Employees.Update}", updateAttr.Policy);

            var deleteMethod = type.GetMethod(nameof(EmployeeController.Delete));
            var deleteAttr = deleteMethod?.GetCustomAttributes(typeof(HasPermissionAttribute), false).FirstOrDefault() as HasPermissionAttribute;
            Assert.NotNull(deleteAttr);
            Assert.Equal($"Permission:{AppPermissions.Employees.Delete}", deleteAttr.Policy);
        }

        [Fact]
        public async Task Create_ShouldReturnStatusCode_WhenServiceSucceeds()
        {
            var request = new CreateEmployeeRequest { Email = "test@onpoint.com" };
            var expectedResult = ServiceResult<EmployeeDetailsDto>.Success(new EmployeeDetailsDto());
            _employeeService.CreateEmployeeAsync(request).Returns(expectedResult);

            var result = await _sut.Create(request) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            Assert.Equal(expectedResult, result.Value);
        }

        [Fact]
        public async Task Create_ShouldReturnErrorStatusCode_WhenServiceFails()
        {
            var request = new CreateEmployeeRequest { Email = "test@onpoint.com" };
            var expectedResult = ServiceResult<EmployeeDetailsDto>.Fail("Some error occurred", 422);
            _employeeService.CreateEmployeeAsync(request).Returns(expectedResult);

            var result = await _sut.Create(request) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(422, result.StatusCode);
            Assert.Equal(expectedResult, result.Value);
        }

        [Fact]
        public async Task GetPaged_ShouldReturnOk_WithResult()
        {
            var request = new PaginationRequest();
            var search = "test";
            var expectedResult = ServiceResult<PagedResult<EmployeeDto>>.Success(PagedResult<EmployeeDto>.Create(new List<EmployeeDto>(), 0, 1, 10));
            _employeeService.GetPagedEmployeesAsync(request, search).Returns(expectedResult);

            var result = await _sut.GetPaged(request, search) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            Assert.Equal(expectedResult, result.Value);
        }

        [Fact]
        public async Task GetById_ShouldReturnOk_WhenSuccess()
        {
            var id = Guid.NewGuid();
            var expectedResult = ServiceResult<EmployeeDetailsDto>.Success(new EmployeeDetailsDto());
            _employeeService.GetEmployeeDetailsAsync(id).Returns(expectedResult);

            var result = await _sut.GetById(id) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            Assert.Equal(expectedResult, result.Value);
        }

        [Fact]
        public async Task GetById_ShouldReturnErrorStatusCode_WhenFailed()
        {
            var id = Guid.NewGuid();
            var expectedResult = ServiceResult<EmployeeDetailsDto>.Fail("Not found", 404);
            _employeeService.GetEmployeeDetailsAsync(id).Returns(expectedResult);

            var result = await _sut.GetById(id) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(404, result.StatusCode);
            Assert.Equal(expectedResult, result.Value);
        }

        [Fact]
        public async Task Update_ShouldReturnOk_WhenSuccess()
        {
            var id = Guid.NewGuid();
            var request = new UpdateEmployeeRequest();
            var expectedResult = ServiceResult<EmployeeDetailsDto>.Success(new EmployeeDetailsDto());
            _employeeService.UpdateEmployeeJobDetailsAsync(id, request).Returns(expectedResult);

            var result = await _sut.Update(id, request) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            Assert.Equal(expectedResult, result.Value);
        }

        [Fact]
        public async Task Update_ShouldReturnErrorStatusCode_WhenFailed()
        {
            var id = Guid.NewGuid();
            var request = new UpdateEmployeeRequest();
            var expectedResult = ServiceResult<EmployeeDetailsDto>.Fail("Validation error", 400);
            _employeeService.UpdateEmployeeJobDetailsAsync(id, request).Returns(expectedResult);

            var result = await _sut.Update(id, request) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
            Assert.Equal(expectedResult, result.Value);
        }

        [Fact]
        public async Task Delete_ShouldReturnOk_WhenSuccess()
        {
            var id = Guid.NewGuid();
            var expectedResult = ServiceResult.Success();
            _employeeService.SoftDeleteEmployeeAsync(id).Returns(expectedResult);

            var result = await _sut.Delete(id) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            Assert.Equal(expectedResult, result.Value);
        }

        [Fact]
        public async Task Delete_ShouldReturnErrorStatusCode_WhenFailed()
        {
            var id = Guid.NewGuid();
            var expectedResult = ServiceResult.Fail("Cannot delete", 403);
            _employeeService.SoftDeleteEmployeeAsync(id).Returns(expectedResult);

            var result = await _sut.Delete(id) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(403, result.StatusCode);
            Assert.Equal(expectedResult, result.Value);
        }
    }
}
