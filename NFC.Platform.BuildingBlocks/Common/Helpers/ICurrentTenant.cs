using System;
using NFC.Platform.Domain.Enums;

namespace NFC.Platform.BuildingBlocks.Common.Helpers
{
    public interface ICurrentTenant
    {
        Guid? TenantId { get; }

        Guid? UserId { get; }

        string? Email { get; }

        AccountType? AccountType { get; }

        bool IsAuthenticated { get; }

        bool IsAdmin { get; }

        void SetCurrentTenant(Guid tenantId, Guid userId);
    }
}
