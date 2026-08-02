using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using NFC.Platform.Application.Interfaces.Services;
using NFC.Platform.BuildingBlocks.Results;
using NFC.Platform.Domain.Entities;

namespace NFC.Platform.Application.Services
{
    /// <summary>
    /// Background service to scan, deactivate past-due tenant subscriptions, and send expiration notifications.
    /// </summary>
    public class SubscriptionExpiryService(
        IUnitOfWork unitOfWork,
        IMessageService messageService,
        IBackgroundJobClient backgroundJobClient) : ISubscriptionExpiryService
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        private readonly IMessageService _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
        private readonly IBackgroundJobClient _backgroundJobClient = backgroundJobClient ?? throw new ArgumentNullException(nameof(backgroundJobClient));

        public async Task<ServiceResult<int>> ProcessExpiredSubscriptionsAsync(CancellationToken cancellationToken = default)
        {
            var subRepo = _unitOfWork.Repository<UserSubscription>();

            // Query all past-due active subscriptions across tenants (ignoring tenant filters for background job execution)
            var expiredSubs = await subRepo.GetQueryable()
                .IgnoreQueryFilters()
                .Include(s => s.SubscriptionPlan)
                .Include(s => s.Tenant)
                    .ThenInclude(t => t.Company)
                        .ThenInclude(c => c!.AdminUser)
                .Include(s => s.User)
                .Where(s => s.IsActive && s.EndDate < DateTime.UtcNow && !s.IsDeleted)
                .ToListAsync(cancellationToken);

            if (expiredSubs.Count == 0)
            {
                return ServiceResult<int>.Success(0, "No expired subscriptions found.");
            }

            var currentCulture = CultureInfo.CurrentUICulture.Name;

            foreach (var sub in expiredSubs)
            {
                sub.IsActive = false;

                // Identify recipient to notify (Company Admin or Individual User)
                var recipient = sub.Tenant?.Company?.AdminUser ?? sub.User;
                if (recipient != null && !string.IsNullOrWhiteSpace(recipient.Email))
                {
                    var planName = sub.SubscriptionPlan != null
                        ? (currentCulture.StartsWith("ar", StringComparison.OrdinalIgnoreCase)
                            ? sub.SubscriptionPlan.NameAr
                            : sub.SubscriptionPlan.NameEn)
                        : _messageService.Get("SubscriptionPlan");

                    _backgroundJobClient.Enqueue<IEmailService>(x =>
                        x.SendSubscriptionExpiredEmailAsync(recipient.Email, planName, currentCulture));
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return ServiceResult<int>.Success(expiredSubs.Count, $"{expiredSubs.Count} expired subscriptions processed and deactivated successfully.");
        }
    }
}
