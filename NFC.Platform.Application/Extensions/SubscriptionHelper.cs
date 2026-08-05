
namespace NFC.Platform.Application.Extensions
{
    public static class SubscriptionHelper
    {
        public static Task<UserSubscription?> GetActiveSubWithPlanAsync(
            IUnitOfWork unitOfWork, Guid tenantId) =>
            unitOfWork.Repository<UserSubscription>()
                .GetQueryable()
                .Include(s => s.SubscriptionPlan)
                .FirstOrDefaultAsync(s =>
                    s.TenantId == tenantId &&
                    s.IsActive &&
                    s.EndDate >= DateTime.UtcNow);

        public static Task<UserSubscription?> GetActiveSubWithTemplatesAsync(
            IUnitOfWork unitOfWork, Guid tenantId) =>
            unitOfWork.Repository<UserSubscription>()
                .GetQueryable()
                .Include(s => s.SubscriptionPlan)
                    .ThenInclude(p => p.PlanTemplates)
                .FirstOrDefaultAsync(s =>
                    s.TenantId == tenantId &&
                    s.IsActive &&
                    s.EndDate >= DateTime.UtcNow);
    }
}
