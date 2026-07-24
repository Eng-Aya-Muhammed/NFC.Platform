namespace NFC.Platform.Tests.Controllers
{
    public class RolesControllerTests
    {
        private readonly IRoleService _roleService;
        private readonly RolesController _sut;

        public RolesControllerTests()
        {
            _roleService = Substitute.For<IRoleService>();
            _sut = new RolesController(_roleService);
        }

        [Fact]
        public void RolesController_ShouldHaveApiControllerAndRouteAttributes()
        {
            var type = typeof(RolesController);
            var apiController = type.GetCustomAttributes(typeof(ApiControllerAttribute), true);
            Assert.NotEmpty(apiController);

            var route = type.GetCustomAttributes(typeof(RouteAttribute), true).Cast<RouteAttribute>().First();
            Assert.Equal("api/company/roles", route.Template);
        }

        [Theory]
        [InlineData(nameof(RolesController.GetRoles), AppPermissions.Roles.View)]
        [InlineData(nameof(RolesController.GetRoleById), AppPermissions.Roles.View)]
        [InlineData(nameof(RolesController.CreateRole), AppPermissions.Roles.Create)]
        [InlineData(nameof(RolesController.UpdateRolePermissions), AppPermissions.Roles.Update)]
        [InlineData(nameof(RolesController.DeleteRole), AppPermissions.Roles.Delete)]
        [InlineData(nameof(RolesController.AssignRoleToUser), AppPermissions.Roles.AssignToUser)]
        [InlineData(nameof(RolesController.RevokeRoleFromUser), AppPermissions.Roles.AssignToUser)]
        [InlineData(nameof(RolesController.GetAvailablePermissions), AppPermissions.Roles.View)]
        public void RolesController_Endpoints_ShouldHaveCorrectPermission(string methodName, string expectedPermission)
        {
            var type = typeof(RolesController);
            var method = type.GetMethod(methodName);
            Assert.NotNull(method);

            var auth = method.GetCustomAttributes(typeof(HasPermissionAttribute), true).Cast<HasPermissionAttribute>().FirstOrDefault();
            Assert.NotNull(auth);
            Assert.Equal($"Permission:{expectedPermission}", auth.Policy);
        }

        [Fact]
        public async Task GetRoles_CallsService_AndReturnsOk()
        {
            var roles = new List<RoleDto>();
            _roleService.GetRolesAsync().Returns(ServiceResult<IReadOnlyList<RoleDto>>.Success(roles));

            var result = await _sut.GetRoles() as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            await _roleService.Received(1).GetRolesAsync();
        }

        [Fact]
        public async Task GetRoleById_CallsService_AndReturnsOk_OnSuccess()
        {
            var id = Guid.NewGuid();
            var role = new RoleDto { Id = id };
            _roleService.GetRoleByIdAsync(id).Returns(ServiceResult<RoleDto>.Success(role));

            var result = await _sut.GetRoleById(id) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            await _roleService.Received(1).GetRoleByIdAsync(id);
        }

        [Fact]
        public async Task GetRoleById_ReturnsError_OnFailure()
        {
            var id = Guid.NewGuid();
            _roleService.GetRoleByIdAsync(id).Returns(ServiceResult<RoleDto>.Fail("Not Found", 404));

            var result = await _sut.GetRoleById(id) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task CreateRole_CallsService_AndReturns201_OnSuccess()
        {
            var request = new CreateRoleRequest();
            var role = new RoleDto();
            _roleService.CreateRoleAsync(request).Returns(new TestServiceResult<RoleDto> { IsSuccess = true, Data = role, StatusCode = 201 });

            var result = await _sut.CreateRole(request) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(201, result.StatusCode);
            await _roleService.Received(1).CreateRoleAsync(request);
        }

        [Fact]
        public async Task UpdateRolePermissions_CallsService_AndReturnsOk_OnSuccess()
        {
            var id = Guid.NewGuid();
            var request = new AssignPermissionsRequest();
            _roleService.UpdateRolePermissionsAsync(id, request).Returns(ServiceResult.Success());

            var result = await _sut.UpdateRolePermissions(id, request) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            await _roleService.Received(1).UpdateRolePermissionsAsync(id, request);
        }

        [Fact]
        public async Task DeleteRole_CallsService_AndReturnsOk_OnSuccess()
        {
            var id = Guid.NewGuid();
            _roleService.DeleteRoleAsync(id).Returns(ServiceResult.Success());

            var result = await _sut.DeleteRole(id) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            await _roleService.Received(1).DeleteRoleAsync(id);
        }
    }
}
