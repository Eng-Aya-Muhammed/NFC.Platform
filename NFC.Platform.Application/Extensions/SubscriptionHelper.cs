
namespace NFC.Platform.Application.Extensions
{
    /// <summary>
    /// Shared helpers for fetching active UserSubscription records.
    /// Follows the same static-helper pattern as <see cref="SubdomainHelper"/>.
    /// </summary>
    public static class SubscriptionHelper
    {
        /// <summary>
        /// Fetches the active subscription (with plan only) for a tenant.
        /// Use for quota checks that only need plan limits (MaxEmployees, MaxTemplateChanges, etc.).
        /// </summary>
        public static Task<UserSubscription?> GetActiveSubWithPlanAsync(
            IUnitOfWork unitOfWork, Guid tenantId) =>
            unitOfWork.Repository<UserSubscription>()
                .GetQueryable()
                .Include(s => s.SubscriptionPlan)
                .FirstOrDefaultAsync(s =>
                    s.TenantId == tenantId &&
                    s.IsActive &&
                    s.EndDate >= DateTime.UtcNow);

        /// <summary>
        /// Fetches the active subscription including PlanTemplates → CardTemplate.
        /// Use when you need to verify whether a specific template is accessible in the tenant's plan.
        /// </summary>
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
