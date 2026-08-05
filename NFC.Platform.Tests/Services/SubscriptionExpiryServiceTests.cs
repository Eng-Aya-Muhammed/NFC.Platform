using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using MockQueryable.NSubstitute;
using NFC.Platform.Application.Interfaces.Services;
using NFC.Platform.Application.Services;
using NFC.Platform.Domain.Entities;
using NSubstitute;
using Xunit;

namespace NFC.Platform.Tests.Services
{
    public class SubscriptionExpiryServiceTests
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IGenericRepository<UserSubscription> _subRepo;
        private readonly IMessageService _messageService;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly SubscriptionExpiryService _sut;

        public SubscriptionExpiryServiceTests()
        {
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _subRepo = Substitute.For<IGenericRepository<UserSubscription>>();
            _messageService = Substitute.For<IMessageService>();
            _backgroundJobClient = Substitute.For<IBackgroundJobClient>();

            _unitOfWork.Repository<UserSubscription>().Returns(_subRepo);

            _sut = new SubscriptionExpiryService(_unitOfWork, _messageService, _backgroundJobClient);
        }

        [Fact]
        public async Task ProcessExpiredSubscriptionsAsync_ReturnsZero_WhenNoSubscriptionsFound()
        {
            var emptyList = new List<UserSubscription>();
            _subRepo.GetQueryable().Returns(emptyList.AsQueryable().BuildMock());

            var result = await _sut.ProcessExpiredSubscriptionsAsync(CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal(0, result.Data);
            await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ProcessExpiredSubscriptionsAsync_DeactivatesExpiredSubscriptions_AndEnqueuesEmails()
        {
            var expiredSub = new UserSubscription
            {
                Id = Guid.NewGuid(),
                TenantId = Guid.NewGuid(),
                IsActive = true,
                StartDate = DateTime.UtcNow.AddDays(-30),
                EndDate = DateTime.UtcNow.AddDays(-1),
                SubscriptionPlan = new SubscriptionPlan { NameAr = "الباقة الفضية", NameEn = "Silver Plan" },
                User = new User { Email = "user@tenant.com" }
            };

            var activeSub = new UserSubscription
            {
                Id = Guid.NewGuid(),
                TenantId = Guid.NewGuid(),
                IsActive = true,
                StartDate = DateTime.UtcNow.AddDays(-10),
                EndDate = DateTime.UtcNow.AddDays(20)
            };

            var list = new List<UserSubscription> { expiredSub, activeSub };
            _subRepo.GetQueryable().Returns(list.AsQueryable().BuildMock());

            var result = await _sut.ProcessExpiredSubscriptionsAsync(CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal(1, result.Data);
            Assert.False(expiredSub.IsActive);
            Assert.True(activeSub.IsActive);

            await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());

            _backgroundJobClient.Received(1).Create(
                Arg.Is<Hangfire.Common.Job>(job =>
                    job.Method.Name == nameof(IEmailService.SendSubscriptionExpiredEmailAsync) &&
                    (string)job.Args[0] == "user@tenant.com"),
                Arg.Any<Hangfire.States.IState>());
        }

        [Fact]
        public async Task ProcessExpiredSubscriptionsAsync_EnqueuesEmailToCompanyAdmin_WhenCompanyExists()
        {
            var companyAdminUser = new User { Email = "admin@company.com" };
            var company = new Company { AdminUser = companyAdminUser };
            var tenant = new Tenant { Company = company };

            var expiredCompanySub = new UserSubscription
            {
                Id = Guid.NewGuid(),
                TenantId = Guid.NewGuid(),
                IsActive = true,
                StartDate = DateTime.UtcNow.AddDays(-30),
                EndDate = DateTime.UtcNow.AddDays(-2),
                SubscriptionPlan = new SubscriptionPlan { NameAr = "باقة الشركات", NameEn = "Corporate Plan" },
                Tenant = tenant,
                User = new User { Email = "regular_employee@company.com" }
            };

            var list = new List<UserSubscription> { expiredCompanySub };
            _subRepo.GetQueryable().Returns(list.AsQueryable().BuildMock());

            var result = await _sut.ProcessExpiredSubscriptionsAsync(CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.False(expiredCompanySub.IsActive);

            _backgroundJobClient.Received(1).Create(
                Arg.Is<Hangfire.Common.Job>(job =>
                    job.Method.Name == nameof(IEmailService.SendSubscriptionExpiredEmailAsync) &&
                    (string)job.Args[0] == "admin@company.com"),
                Arg.Any<Hangfire.States.IState>());
        }
    }
}
