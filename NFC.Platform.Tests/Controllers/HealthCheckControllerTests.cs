namespace NFC.Platform.Tests.Controllers
{
    public class HealthCheckControllerTests
    {
        [Fact]
        public async Task CheckHealth_ReturnsOk_WhenDatabaseCanConnect()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer("Server=dummy;Database=dummy;Trusted_Connection=True;TrustServerCertificate=True;")
                .Options;

            var currentUserService = Substitute.For<ICurrentUserService>();
            var dateTimeProvider = Substitute.For<IDateTimeProvider>();
            var interceptor = new AuditableEntitySaveChangesInterceptor(currentUserService, dateTimeProvider);
            var currentTenant = Substitute.For<ICurrentTenant>();

            var context = Substitute.For<ApplicationDbContext>(options, interceptor, currentTenant);
            var dbFacade = Substitute.For<DatabaseFacade>(context);
            dbFacade.CanConnectAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));

            context.Database.Returns(dbFacade);

            var sut = new HealthCheckController(context);

            var result = await sut.CheckHealth() as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);

            var json = System.Text.Json.JsonSerializer.Serialize(result.Value);
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;
            Assert.Equal("Healthy", root.GetProperty("Status").GetString());
            Assert.Equal("Connected", root.GetProperty("Database").GetString());
        }

        [Fact]
        public async Task CheckHealth_Returns503_WhenDatabaseCannotConnect()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer("Server=dummy;Database=dummy;Trusted_Connection=True;TrustServerCertificate=True;")
                .Options;

            var currentUserService = Substitute.For<ICurrentUserService>();
            var dateTimeProvider = Substitute.For<IDateTimeProvider>();
            var interceptor = new AuditableEntitySaveChangesInterceptor(currentUserService, dateTimeProvider);
            var currentTenant = Substitute.For<ICurrentTenant>();

            var context = Substitute.For<ApplicationDbContext>(options, interceptor, currentTenant);
            var dbFacade = Substitute.For<DatabaseFacade>(context);
            dbFacade.CanConnectAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(false));

            context.Database.Returns(dbFacade);

            var sut = new HealthCheckController(context);

            var result = await sut.CheckHealth() as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(503, result.StatusCode);

            var json = System.Text.Json.JsonSerializer.Serialize(result.Value);
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;
            Assert.Equal("Unhealthy", root.GetProperty("Status").GetString());
            Assert.Equal("Disconnected", root.GetProperty("Database").GetString());
        }

        [Fact]
        public async Task CheckHealth_Returns503_WhenExceptionIsThrown()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer("Server=dummy;Database=dummy;Trusted_Connection=True;TrustServerCertificate=True;")
                .Options;

            var currentUserService = Substitute.For<ICurrentUserService>();
            var dateTimeProvider = Substitute.For<IDateTimeProvider>();
            var interceptor = new AuditableEntitySaveChangesInterceptor(currentUserService, dateTimeProvider);
            var currentTenant = Substitute.For<ICurrentTenant>();

            var context = Substitute.For<ApplicationDbContext>(options, interceptor, currentTenant);
            var dbFacade = Substitute.For<DatabaseFacade>(context);
            dbFacade.CanConnectAsync(Arg.Any<CancellationToken>()).Returns<Task<bool>>(_ => throw new Exception("Connection error"));

            context.Database.Returns(dbFacade);

            var sut = new HealthCheckController(context);

            var result = await sut.CheckHealth() as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(503, result.StatusCode);

            var json = System.Text.Json.JsonSerializer.Serialize(result.Value);
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;
            Assert.Equal("Unhealthy", root.GetProperty("Status").GetString());
            Assert.Equal("ConnectionFailed", root.GetProperty("Database").GetString());
            Assert.Equal("Connection error", root.GetProperty("Error").GetString());
        }
    }
}
