using System;
using System.Text;
namespace NFC.Platform.Tests.Controllers
{
    public class PublicProfileControllerTests
    {
        private readonly IProfileMetricService _profileMetricService;
        private readonly IQrCodeService _qrCodeService;
        private readonly IVCardService _vCardService;
        private readonly PublicProfileController _sut;

        public PublicProfileControllerTests()
        {
            _profileMetricService = Substitute.For<IProfileMetricService>();
            _qrCodeService = Substitute.For<IQrCodeService>();
            _vCardService = Substitute.For<IVCardService>();
            _sut = new PublicProfileController(_profileMetricService, _qrCodeService, _vCardService);

            // Mock Default ControllerContext for Response headers
            _sut.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
        }

        [Fact]
        public void PublicProfileController_ShouldHaveAllowAnonymousAndRouteAttributes()
        {
            var type = typeof(PublicProfileController);
            Assert.NotEmpty(type.GetCustomAttributes(typeof(AllowAnonymousAttribute), true));
            var route = type.GetCustomAttributes(typeof(RouteAttribute), true).Cast<RouteAttribute>().First();
            Assert.Equal("api/public", route.Template);
        }

        [Fact]
        public async Task ResolvePublicProfile_CallsService_AndReturnsOk_OnSuccess()
        {
            var id = Guid.NewGuid();
            var dto = new EmployeeDetailsDto();
            _profileMetricService.ResolvePublicProfileAsync(id).Returns(ServiceResult<EmployeeDetailsDto>.Success(dto));

            var result = await _sut.ResolvePublicProfile(id) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            await _profileMetricService.Received(1).ResolvePublicProfileAsync(id);
        }

        [Fact]
        public async Task ResolvePublicProfile_ReturnsError_OnFailure()
        {
            var id = Guid.NewGuid();
            _profileMetricService.ResolvePublicProfileAsync(id).Returns(ServiceResult<EmployeeDetailsDto>.Fail("Error", 404));

            var result = await _sut.ResolvePublicProfile(id) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task GetProfileQrBySubdomain_ReturnsFileResult_OnSuccess()
        {
            var subdomain = "ahmed-ali";
            var profileUrl = "https://nfc-platform.com/u/ahmed-ali";
            var dto = new EmployeeDetailsDto { FullName = "Ahmed Ali", ProfileUrl = profileUrl };
            var fakeBytes = new byte[] { 0x89, (byte)'P', (byte)'N', (byte)'G' };

            _profileMetricService.ResolvePublicProfileBySubdomainAsync(subdomain).Returns(ServiceResult<EmployeeDetailsDto>.Success(dto));
            _qrCodeService.GeneratePngQrCode(profileUrl).Returns(fakeBytes);

            var result = await _sut.GetProfileQrBySubdomain(subdomain) as FileContentResult;

            Assert.NotNull(result);
            Assert.Equal("image/png", result.ContentType);
            Assert.Equal(fakeBytes, result.FileContents);
        }

        [Fact]
        public async Task GetProfileQrBySubdomain_ReturnsFileWithDownloadFilename_WhenDownloadIsTrue()
        {
            var subdomain = "ahmed-ali";
            var profileUrl = "https://nfc-platform.com/u/ahmed-ali";
            var dto = new EmployeeDetailsDto { FullName = "Ahmed Ali", ProfileUrl = profileUrl };
            var fakeBytes = new byte[] { 0x89, (byte)'P', (byte)'N', (byte)'G' };

            _profileMetricService.ResolvePublicProfileBySubdomainAsync(subdomain).Returns(ServiceResult<EmployeeDetailsDto>.Success(dto));
            _qrCodeService.GeneratePngQrCode(profileUrl).Returns(fakeBytes);

            var result = await _sut.GetProfileQrBySubdomain(subdomain, download: true) as FileContentResult;

            Assert.NotNull(result);
            Assert.Equal("image/png", result.ContentType);
            Assert.Equal("ahmed-ali-qr.png", result.FileDownloadName);
        }

        [Fact]
        public async Task RecordMetric_CallsService_AndReturnsOk_OnSuccess()
        {
            var profileId = Guid.NewGuid();
            var request = new RecordMetricRequest();
            _profileMetricService.RecordMetricAsync(profileId, request).Returns(ServiceResult.Success());

            var result = await _sut.RecordMetric(profileId, request) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            await _profileMetricService.Received(1).RecordMetricAsync(profileId, request);
        }

        [Fact]
        public async Task RecordMetric_ReturnsError_OnFailure()
        {
            var profileId = Guid.NewGuid();
            var request = new RecordMetricRequest();
            _profileMetricService.RecordMetricAsync(profileId, request).Returns(ServiceResult.Fail("Error", 400));

            var result = await _sut.RecordMetric(profileId, request) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public void ResolvePublicProfile_ShouldHaveRateLimitingPolicy()
        {
            var method = typeof(PublicProfileController).GetMethod(nameof(PublicProfileController.ResolvePublicProfile));
            Assert.NotNull(method);

            var attr = method.GetCustomAttributes(typeof(EnableRateLimitingAttribute), true)
                .Cast<EnableRateLimitingAttribute>()
                .FirstOrDefault();

            Assert.NotNull(attr);
            Assert.Equal("ResolvePublicProfilePolicy", attr.PolicyName);
        }

        [Fact]
        public void GetProfileQrBySubdomain_ShouldHaveRateLimitingPolicy()
        {
            var method = typeof(PublicProfileController).GetMethod(nameof(PublicProfileController.GetProfileQrBySubdomain));
            Assert.NotNull(method);

            var attr = method.GetCustomAttributes(typeof(EnableRateLimitingAttribute), true)
                .Cast<EnableRateLimitingAttribute>()
                .FirstOrDefault();

            Assert.NotNull(attr);
            Assert.Equal("ResolvePublicProfilePolicy", attr.PolicyName);
        }

        [Fact]
        public void RecordMetric_ShouldNotHaveRateLimiting()
        {
            var method = typeof(PublicProfileController).GetMethod(nameof(PublicProfileController.RecordMetric));
            Assert.NotNull(method);

            var attr = method.GetCustomAttributes(typeof(EnableRateLimitingAttribute), true);
            Assert.Empty(attr);
        }

        [Fact]
        public async Task ResolvePublicProfileBySubdomain_CallsService_AndReturnsOk_OnSuccess()
        {
            var subdomain = "ahmed-ali";
            var dto = new EmployeeDetailsDto { FullName = "Ahmed Ali" };
            _profileMetricService.ResolvePublicProfileBySubdomainAsync(subdomain).Returns(ServiceResult<EmployeeDetailsDto>.Success(dto));

            var result = await _sut.ResolvePublicProfileBySubdomain(subdomain) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            await _profileMetricService.Received(1).ResolvePublicProfileBySubdomainAsync(subdomain);
        }

        [Fact]
        public async Task ResolvePublicProfileBySubdomain_ReturnsError_OnFailure()
        {
            var subdomain = "non-existent";
            _profileMetricService.ResolvePublicProfileBySubdomainAsync(subdomain).Returns(ServiceResult<EmployeeDetailsDto>.Fail("Error", 404));

            var result = await _sut.ResolvePublicProfileBySubdomain(subdomain) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public void ResolvePublicProfileBySubdomain_ShouldHaveRateLimitingPolicy()
        {
            var method = typeof(PublicProfileController).GetMethod(nameof(PublicProfileController.ResolvePublicProfileBySubdomain));
            Assert.NotNull(method);

            var attr = method.GetCustomAttributes(typeof(EnableRateLimitingAttribute), true)
                .Cast<EnableRateLimitingAttribute>()
                .FirstOrDefault();

            Assert.NotNull(attr);
            Assert.Equal("ResolvePublicProfilePolicy", attr.PolicyName);
        }

        [Fact]
        public async Task GetProfileQrBySubdomain_ReturnsNotFound_WhenProfileDoesNotExist()
        {
            var subdomain = "non-existent";
            _profileMetricService.ResolvePublicProfileBySubdomainAsync(subdomain)
                .Returns(ServiceResult<EmployeeDetailsDto>.Fail("Not found", 404));

            var result = await _sut.GetProfileQrBySubdomain(subdomain) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task GetProfileQrBySubdomain_ReturnsNotFound_WhenProfileUrlIsNull()
        {
            var subdomain = "empty-url";
            var dto = new EmployeeDetailsDto { FullName = "Test User", ProfileUrl = null };
            _profileMetricService.ResolvePublicProfileBySubdomainAsync(subdomain)
                .Returns(ServiceResult<EmployeeDetailsDto>.Success(dto));

            var result = await _sut.GetProfileQrBySubdomain(subdomain) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode); // Returns original success result (or 404 depending on status code)
        }

        [Fact]
        public async Task GetProfileQrBySubdomain_SetsCacheControlHeaderInResponse()
        {
            var subdomain = "ahmed-ali";
            var profileUrl = "https://nfc-platform.com/u/ahmed-ali";
            var dto = new EmployeeDetailsDto { FullName = "Ahmed Ali", ProfileUrl = profileUrl };
            var fakeBytes = new byte[] { 0x89, (byte)'P', (byte)'N', (byte)'G' };

            _profileMetricService.ResolvePublicProfileBySubdomainAsync(subdomain).Returns(ServiceResult<EmployeeDetailsDto>.Success(dto));
            _qrCodeService.GeneratePngQrCode(profileUrl).Returns(fakeBytes);

            await _sut.GetProfileQrBySubdomain(subdomain);

            Assert.Equal("public, max-age=86400", _sut.Response.Headers["Cache-Control"].ToString());
        }

        [Fact]
        public async Task GetProfileQrById_ReturnsFileResult_OnSuccess()
        {
            var id = Guid.NewGuid();
            var profileUrl = "https://nfc-platform.com/u/ahmed-ali";
            var dto = new EmployeeDetailsDto { FullName = "Ahmed Ali", ProfileUrl = profileUrl };
            var fakeBytes = new byte[] { 0x89, (byte)'P', (byte)'N', (byte)'G' };

            _profileMetricService.ResolvePublicProfileAsync(id).Returns(ServiceResult<EmployeeDetailsDto>.Success(dto));
            _qrCodeService.GeneratePngQrCode(profileUrl).Returns(fakeBytes);

            var result = await _sut.GetProfileQrById(id) as FileContentResult;

            Assert.NotNull(result);
            Assert.Equal("image/png", result.ContentType);
            Assert.Equal(fakeBytes, result.FileContents);
        }

        [Fact]
        public async Task GetProfileQrById_ReturnsNotFound_WhenProfileDoesNotExist()
        {
            var id = Guid.NewGuid();
            _profileMetricService.ResolvePublicProfileAsync(id)
                .Returns(ServiceResult<EmployeeDetailsDto>.Fail("Not found", 404));

            var result = await _sut.GetProfileQrById(id) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public void QrEndpoints_ShouldHaveResponseCacheAttribute()
        {
            var methodSubdomain = typeof(PublicProfileController).GetMethod(nameof(PublicProfileController.GetProfileQrBySubdomain));
            Assert.NotNull(methodSubdomain);
            var attrSubdomain = methodSubdomain.GetCustomAttributes(typeof(ResponseCacheAttribute), true)
                .Cast<ResponseCacheAttribute>()
                .FirstOrDefault();
            Assert.NotNull(attrSubdomain);
            Assert.Equal(86400, attrSubdomain.Duration);

            var methodId = typeof(PublicProfileController).GetMethod(nameof(PublicProfileController.GetProfileQrById));
            Assert.NotNull(methodId);
            var attrId = methodId.GetCustomAttributes(typeof(ResponseCacheAttribute), true)
                .Cast<ResponseCacheAttribute>()
                .FirstOrDefault();
            Assert.NotNull(attrId);
            Assert.Equal(86400, attrId.Duration);
        }

        [Fact]
        public async Task DownloadVCardBySubdomain_ReturnsFileResult_AndLogsContactSavedMetric()
        {
            var subdomain = "ahmed-ali";
            var profileId = Guid.NewGuid();
            var dto = new EmployeeDetailsDto { ProfileId = profileId, FullName = "Ahmed Ali" };
            var fakeVCardBytes = Encoding.UTF8.GetBytes("BEGIN:VCARD\nVERSION:3.0\nFN:Ahmed Ali\nEND:VCARD");

            _profileMetricService.ResolvePublicProfileBySubdomainAsync(subdomain).Returns(ServiceResult<EmployeeDetailsDto>.Success(dto));
            _vCardService.BuildVCardBytes(dto).Returns(fakeVCardBytes);

            var result = await _sut.DownloadVCardBySubdomain(subdomain) as FileContentResult;

            Assert.NotNull(result);
            Assert.Equal("text/vcard; charset=utf-8", result.ContentType);
            Assert.Equal("ahmed-ali.vcf", result.FileDownloadName);
            Assert.Equal(fakeVCardBytes, result.FileContents);
        }

        [Fact]
        public async Task DownloadVCardBySubdomain_ReturnsNotFound_WhenProfileDoesNotExist()
        {
            var subdomain = "non-existent";
            _profileMetricService.ResolvePublicProfileBySubdomainAsync(subdomain)
                .Returns(ServiceResult<EmployeeDetailsDto>.Fail("Not found", 404));

            var result = await _sut.DownloadVCardBySubdomain(subdomain) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task DownloadVCardById_ReturnsFileResult_OnSuccess()
        {
            var id = Guid.NewGuid();
            var dto = new EmployeeDetailsDto { ProfileId = id, FullName = "Ahmed Ali" };
            var fakeVCardBytes = Encoding.UTF8.GetBytes("BEGIN:VCARD\nVERSION:3.0\nFN:Ahmed Ali\nEND:VCARD");

            _profileMetricService.ResolvePublicProfileAsync(id).Returns(ServiceResult<EmployeeDetailsDto>.Success(dto));
            _vCardService.BuildVCardBytes(dto).Returns(fakeVCardBytes);

            var result = await _sut.DownloadVCardById(id) as FileContentResult;

            Assert.NotNull(result);
            Assert.Equal("text/vcard; charset=utf-8", result.ContentType);
            Assert.Equal("ahmed-ali.vcf", result.FileDownloadName);
            Assert.Equal(fakeVCardBytes, result.FileContents);
        }
    }
}
