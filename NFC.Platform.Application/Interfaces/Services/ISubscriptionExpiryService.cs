using System.Threading;
using System.Threading.Tasks;
using NFC.Platform.BuildingBlocks.Results;

namespace NFC.Platform.Application.Interfaces.Services
{
    /// <summary>
    /// Contract for background job processing of expired tenant subscriptions.
    /// </summary>
    public interface ISubscriptionExpiryService
    {
        /// <summary>
        /// Scans all active subscriptions across tenants, deactivates past-due subscriptions (EndDate &lt; UtcNow),
        /// and enqueues expiration notification emails.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Service result containing the number of subscriptions processed and deactivated.</returns>
        Task<ServiceResult<int>> ProcessExpiredSubscriptionsAsync(CancellationToken cancellationToken = default);
    }
}
