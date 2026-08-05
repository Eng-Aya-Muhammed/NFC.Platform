using System.Threading;
using System.Threading.Tasks;
using NFC.Platform.BuildingBlocks.Results;

namespace NFC.Platform.Application.Interfaces.Services
{
    public interface ISubscriptionExpiryService
    {
        Task<ServiceResult<int>> ProcessExpiredSubscriptionsAsync(CancellationToken cancellationToken = default);
    }
}
