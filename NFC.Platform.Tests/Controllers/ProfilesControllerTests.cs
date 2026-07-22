using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NFC.Platform.API.Controllers;
using NFC.Platform.Application.DTOs.Employee;
using NFC.Platform.Application.DTOs.Profile;
using NFC.Platform.Application.Interfaces.Services;
using NFC.Platform.BuildingBlocks.Common.Helpers;
using NFC.Platform.BuildingBlocks.Results;
using NSubstitute;
using Xunit;

namespace NFC.Platform.Tests.Controllers
{
    public class ProfilesControllerTests
    {
        private readonly IProfileService _profileService;
        private readonly ICurrentTenant _currentTenant;
        private readonly ProfilesController _sut;

        public ProfilesControllerTests()
        {
            _profileService = Substitute.For<IProfileService>();
            _currentTenant = Substitute.For<ICurrentTenant>();
            _sut = new ProfilesController(_profileService, _currentTenant);
        }

        [Fact]
        public void ProfilesController_ShouldHaveAuthorizeAndRouteAttributes()
        {
            var type = typeof(ProfilesController);
            Assert.NotEmpty(type.GetCustomAttributes(typeof(AuthorizeAttribute), true));
            var route = type.GetCustomAttributes(typeof(RouteAttribute), true).Cast<RouteAttribute>().First();
            Assert.Equal("api/user/profile", route.Template);
        }

        [Fact]
        public async Task GetProfile_ReturnsUnauthorized_WhenUserIdIsNull()
        {
            _currentTenant.UserId.Returns((Guid?)null);

            var result = await _sut.GetProfile() as UnauthorizedObjectResult;

            Assert.NotNull(result);
            Assert.Equal(401, result.StatusCode);
        }

        [Fact]
        public async Task GetProfile_CallsService_AndReturnsOk_OnSuccess()
        {
            var userId = Guid.NewGuid();
            _currentTenant.UserId.Returns(userId);
            var dto = new EmployeeDetailsDto();
            _profileService.GetProfileAsync(userId).Returns(ServiceResult<EmployeeDetailsDto>.Success(dto));

            var result = await _sut.GetProfile() as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            await _profileService.Received(1).GetProfileAsync(userId);
        }

        [Fact]
        public async Task GetProfile_ReturnsError_OnFailure()
        {
            var userId = Guid.NewGuid();
            _currentTenant.UserId.Returns(userId);
            _profileService.GetProfileAsync(userId).Returns(ServiceResult<EmployeeDetailsDto>.Fail("Error", 400));

            var result = await _sut.GetProfile() as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public async Task UpdateProfile_ReturnsUnauthorized_WhenUserIdIsNull()
        {
            _currentTenant.UserId.Returns((Guid?)null);

            var result = await _sut.UpdateProfile(new UpdateMyProfileRequest()) as UnauthorizedObjectResult;

            Assert.NotNull(result);
            Assert.Equal(401, result.StatusCode);
        }

        [Fact]
        public async Task UpdateProfile_CallsService_AndReturnsOk_OnSuccess()
        {
            var userId = Guid.NewGuid();
            _currentTenant.UserId.Returns(userId);
            var request = new UpdateMyProfileRequest();
            var dto = new EmployeeDetailsDto();
            _profileService.UpdateProfileAsync(userId, request).Returns(ServiceResult<EmployeeDetailsDto>.Success(dto));

            var result = await _sut.UpdateProfile(request) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            await _profileService.Received(1).UpdateProfileAsync(userId, request);
        }

        [Fact]
        public async Task UpdateProfile_ReturnsError_OnFailure()
        {
            var userId = Guid.NewGuid();
            _currentTenant.UserId.Returns(userId);
            var request = new UpdateMyProfileRequest();
            _profileService.UpdateProfileAsync(userId, request).Returns(ServiceResult<EmployeeDetailsDto>.Fail("Error", 400));

            var result = await _sut.UpdateProfile(request) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public async Task SynchronizeLinks_ReturnsUnauthorized_WhenUserIdIsNull()
        {
            _currentTenant.UserId.Returns((Guid?)null);

            var result = await _sut.SynchronizeLinks(new SynchronizeLinksRequest()) as UnauthorizedObjectResult;

            Assert.NotNull(result);
            Assert.Equal(401, result.StatusCode);
        }

        [Fact]
        public async Task SynchronizeLinks_CallsService_AndReturnsOk_OnSuccess()
        {
            var userId = Guid.NewGuid();
            _currentTenant.UserId.Returns(userId);
            var request = new SynchronizeLinksRequest();
            var dto = new EmployeeDetailsDto();
            _profileService.SynchronizeLinksAsync(userId, request).Returns(ServiceResult<EmployeeDetailsDto>.Success(dto));

            var result = await _sut.SynchronizeLinks(request) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            await _profileService.Received(1).SynchronizeLinksAsync(userId, request);
        }

        [Fact]
        public async Task SynchronizeLinks_ReturnsError_OnFailure()
        {
            var userId = Guid.NewGuid();
            _currentTenant.UserId.Returns(userId);
            var request = new SynchronizeLinksRequest();
            _profileService.SynchronizeLinksAsync(userId, request).Returns(ServiceResult<EmployeeDetailsDto>.Fail("Error", 400));

            var result = await _sut.SynchronizeLinks(request) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
        }
        [Fact]
        public async Task ApplyPublicProfileTemplate_ReturnsOk_OnSuccess()
        {
            var userId = Guid.NewGuid();
            var templateId = Guid.NewGuid();
            _currentTenant.UserId.Returns(userId);
            _profileService.UpdateProfileTemplateAsync(userId, templateId).Returns(ServiceResult<EmployeeDetailsDto>.Success(new EmployeeDetailsDto()));

            var result = await _sut.ApplyPublicProfileTemplate(templateId) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            await _profileService.Received(1).UpdateProfileTemplateAsync(userId, templateId);
        }

        [Fact]
        public async Task RemovePublicProfileTemplate_ReturnsOk_OnSuccess()
        {
            var userId = Guid.NewGuid();
            _currentTenant.UserId.Returns(userId);
            _profileService.UpdateProfileTemplateAsync(userId, null).Returns(ServiceResult<EmployeeDetailsDto>.Success(new EmployeeDetailsDto()));

            var result = await _sut.RemovePublicProfileTemplate() as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            await _profileService.Received(1).UpdateProfileTemplateAsync(userId, null);
        }
    }
}
