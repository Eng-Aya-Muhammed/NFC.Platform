namespace NFC.Platform.Tests.Services
{
    public class RoleServiceTests
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentTenant _currentTenant;
        private readonly IPermissionCacheService _permissionCache;
        private readonly IMessageService _messageService;
        private readonly RoleService _sut;

        public RoleServiceTests()
        {
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _currentTenant = Substitute.For<ICurrentTenant>();
            _permissionCache = Substitute.For<IPermissionCacheService>();
            _messageService = Substitute.For<IMessageService>();
            _sut = new RoleService(_unitOfWork, _currentTenant, _permissionCache, _messageService);
        }

        [Fact]
        public async Task CreateRole_ReturnsUnauthorized_WhenTenantIdIsNull()
        {
            _currentTenant.TenantId.Returns((Guid?)null);

            var result = await _sut.CreateRoleAsync(new CreateRoleRequest());

            Assert.False(result.IsSuccess);
            Assert.Equal(401, result.StatusCode);
        }

        [Fact]
        public async Task CreateRole_ReturnsFail_WhenRoleAlreadyExists()
        {
            var tenantId = Guid.NewGuid();
            _currentTenant.TenantId.Returns(tenantId);
            _messageService.Get(Arg.Any<string>()).Returns("Exists");

            var roleRepo = Substitute.For<IGenericRepository<Role>>();
            roleRepo.FindAsync(Arg.Any<Expression<Func<Role, bool>>>()).Returns(new List<Role> { new Role() });
            _unitOfWork.Repository<Role>().Returns(roleRepo);

            var result = await _sut.CreateRoleAsync(new CreateRoleRequest { Name = "Admin" });

            Assert.False(result.IsSuccess);
            Assert.Equal("Exists", result.Message);
        }

        [Fact]
        public async Task CreateRole_ReturnsSuccess_WhenValid()
        {
            var tenantId = Guid.NewGuid();
            _currentTenant.TenantId.Returns(tenantId);
            
            var roleRepo = Substitute.For<IGenericRepository<Role>>();
            roleRepo.FindAsync(Arg.Any<Expression<Func<Role, bool>>>()).Returns(new List<Role>());
            _unitOfWork.Repository<Role>().Returns(roleRepo);

            var result = await _sut.CreateRoleAsync(new CreateRoleRequest { Name = "NewRole", Permissions = new List<string> { AppPermissions.Company.View } });

            Assert.True(result.IsSuccess);
            Assert.Equal("NewRole", result.Data.Name);
            await _unitOfWork.Received(1).SaveChangesAsync();
        }

        [Fact]
        public async Task GetRoleById_ReturnsNotFound_WhenNotExists()
        {
            var roleRepo = Substitute.For<IGenericRepository<Role>>();
            roleRepo.FindAsync(Arg.Any<Expression<Func<Role, bool>>>()).Returns(new List<Role>());
            _unitOfWork.Repository<Role>().Returns(roleRepo);
            _messageService.Get(Arg.Any<string>()).Returns("Not Found");

            var result = await _sut.GetRoleByIdAsync(Guid.NewGuid());

            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task AssignRoleToUser_ReturnsFail_WhenUserAlreadyHasRole()
        {
            var tenantId = Guid.NewGuid();
            _currentTenant.TenantId.Returns(tenantId);
            
            var roleRepo = Substitute.For<IGenericRepository<Role>>();
            roleRepo.FindAsync(Arg.Any<Expression<Func<Role, bool>>>()).Returns(new List<Role> { new Role() });
            _unitOfWork.Repository<Role>().Returns(roleRepo);

            var userRoleRepo = Substitute.For<IGenericRepository<UserRole>>();
            userRoleRepo.FindAsync(Arg.Any<Expression<Func<UserRole, bool>>>()).Returns(new List<UserRole> { new UserRole() });
            _unitOfWork.Repository<UserRole>().Returns(userRoleRepo);
            _messageService.Get(Arg.Any<string>()).Returns("UserAlreadyHasRole");

            var result = await _sut.AssignRoleToUserAsync(Guid.NewGuid(), Guid.NewGuid());

            Assert.False(result.IsSuccess);
            Assert.Equal("UserAlreadyHasRole", result.Message);
        }

        [Fact]
        public async Task AssignRoleToUser_ReturnsSuccess_WhenValid()
        {
            var tenantId = Guid.NewGuid();
            _currentTenant.TenantId.Returns(tenantId);
            
            var roleRepo = Substitute.For<IGenericRepository<Role>>();
            roleRepo.FindAsync(Arg.Any<Expression<Func<Role, bool>>>()).Returns(new List<Role> { new Role() });
            _unitOfWork.Repository<Role>().Returns(roleRepo);

            var userRoleRepo = Substitute.For<IGenericRepository<UserRole>>();
            userRoleRepo.FindAsync(Arg.Any<Expression<Func<UserRole, bool>>>()).Returns(new List<UserRole>());
            _unitOfWork.Repository<UserRole>().Returns(userRoleRepo);
            _messageService.Get(Arg.Any<string>()).Returns("Success");

            var userId = Guid.NewGuid();
            var result = await _sut.AssignRoleToUserAsync(userId, Guid.NewGuid());

            Assert.True(result.IsSuccess);
            await _unitOfWork.Received(1).SaveChangesAsync();
            _permissionCache.Received(1).InvalidateUser(userId);
        }
    }
}
