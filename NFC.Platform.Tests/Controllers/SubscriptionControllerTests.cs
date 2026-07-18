using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NFC.Platform.API.Controllers;
using NFC.Platform.Application.DTOs;
using NFC.Platform.Application.Interfaces.Services;
using NFC.Platform.BuildingBlocks.Common.Constants;
using NFC.Platform.BuildingBlocks.Results;
using NSubstitute;
using Xunit;

namespace NFC.Platform.Tests.Controllers
{
    public class SubscriptionControllerTests
    {
        private readonly ISubscriptionService _subscriptionService;
        private readonly SubscriptionController _sut;

        public SubscriptionControllerTests()
        {
            _subscriptionService = Substitute.For<ISubscriptionService>();
            _sut = new SubscriptionController(_subscriptionService);
        }

        [Fact]
        public void SubscriptionController_ShouldHaveApiControllerAndRouteAttributes()
        {
            var type = typeof(SubscriptionController);
            Assert.NotEmpty(type.GetCustomAttributes(typeof(ApiControllerAttribute), true));
            var route = type.GetCustomAttributes(typeof(RouteAttribute), true).Cast<RouteAttribute>().First();
            Assert.Equal("api/subscription", route.Template);
        }

        [Fact]
        public async Task GetPlans_CallsService_AndReturnsOk()
        {
            var plans = new List<SubscriptionPlanDto>();
            _subscriptionService.GetPlansAsync().Returns(ServiceResult<IReadOnlyList<SubscriptionPlanDto>>.Success(plans));

            var result = await _sut.GetPlans() as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            await _subscriptionService.Received(1).GetPlansAsync();
        }

        [Fact]
        public async Task GetCurrent_CallsService_AndReturnsOk_OnSuccess()
        {
            var dto = new UserSubscriptionDto();
            _subscriptionService.GetCurrentSubscriptionAsync().Returns(ServiceResult<UserSubscriptionDto>.Success(dto));

            var result = await _sut.GetCurrent() as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            await _subscriptionService.Received(1).GetCurrentSubscriptionAsync();
        }

        [Fact]
        public async Task GetCurrent_ReturnsError_OnFailure()
        {
            _subscriptionService.GetCurrentSubscriptionAsync().Returns(ServiceResult<UserSubscriptionDto>.Fail("Error", 400));

            var result = await _sut.GetCurrent() as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public async Task GetHistory_CallsService_AndReturnsOk_OnSuccess()
        {
            var list = new List<UserSubscriptionDto>();
            _subscriptionService.GetSubscriptionHistoryAsync().Returns(ServiceResult<IReadOnlyList<UserSubscriptionDto>>.Success(list));

            var result = await _sut.GetHistory() as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            await _subscriptionService.Received(1).GetSubscriptionHistoryAsync();
        }

        [Fact]
        public async Task GetHistory_ReturnsError_OnFailure()
        {
            _subscriptionService.GetSubscriptionHistoryAsync().Returns(ServiceResult<IReadOnlyList<UserSubscriptionDto>>.Fail("Error", 400));

            var result = await _sut.GetHistory() as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public async Task Subscribe_CallsService_AndReturnsOk_OnSuccess()
        {
            var request = new SubscribeRequest();
            var dto = new UserSubscriptionDto();
            _subscriptionService.SubscribeAsync(request).Returns(ServiceResult<UserSubscriptionDto>.Success(dto));

            var result = await _sut.Subscribe(request) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            await _subscriptionService.Received(1).SubscribeAsync(request);
        }

        [Fact]
        public async Task Subscribe_ReturnsError_OnFailure()
        {
            var request = new SubscribeRequest();
            _subscriptionService.SubscribeAsync(request).Returns(ServiceResult<UserSubscriptionDto>.Fail("Error", 400));

            var result = await _sut.Subscribe(request) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public async Task Renew_CallsService_AndReturnsOk_OnSuccess()
        {
            var request = new RenewSubscriptionRequest();
            var dto = new UserSubscriptionDto();
            _subscriptionService.RenewSubscriptionAsync(request).Returns(ServiceResult<UserSubscriptionDto>.Success(dto));

            var result = await _sut.Renew(request) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            await _subscriptionService.Received(1).RenewSubscriptionAsync(request);
        }

        [Fact]
        public async Task Renew_ReturnsError_OnFailure()
        {
            var request = new RenewSubscriptionRequest();
            _subscriptionService.RenewSubscriptionAsync(request).Returns(ServiceResult<UserSubscriptionDto>.Fail("Error", 400));

            var result = await _sut.Renew(request) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
        }

        [Theory]
        [InlineData(nameof(SubscriptionController.GetCurrent))]
        [InlineData(nameof(SubscriptionController.GetHistory))]
        [InlineData(nameof(SubscriptionController.Subscribe))]
        [InlineData(nameof(SubscriptionController.Renew))]
        public void SubscriptionController_ProtectedEndpoints_ShouldHaveCompanyAdminOnlyPolicy(string methodName)
        {
            var type = typeof(SubscriptionController);
            var method = type.GetMethod(methodName);
            Assert.NotNull(method);

            var auth = method.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>().FirstOrDefault();
            Assert.NotNull(auth);
            Assert.Equal(AppPolicies.CompanyAdminOnly, auth.Policy);
        }
    }
}
