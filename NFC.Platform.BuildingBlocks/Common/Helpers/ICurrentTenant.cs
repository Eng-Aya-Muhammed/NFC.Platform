using System;

namespace NFC.Platform.BuildingBlocks.Common.Helpers
{
    /// <summary>
    /// Service contract to retrieve identity and tenant information for the currently active request.
    /// </summary>
    public interface ICurrentTenant
    {
        /// <summary>
        /// Gets the unique identifier of the current tenant. Returns null if not authenticated.
        /// </summary>
        Guid? TenantId { get; }

        /// <summary>
        /// Gets the unique identifier of the current user. Returns null if not authenticated.
        /// </summary>
        Guid? UserId { get; }

        /// <summary>
        /// Gets the email address of the current user. Returns null if not authenticated.
        /// </summary>
        string? Email { get; }

        /// <summary>
        /// Gets a value indicating whether the current request is authenticated.
        /// </summary>
        bool IsAuthenticated { get; }

        /// <summary>
        /// Gets a value indicating whether the current user is a SuperAdmin (bypassing tenant filters).
        /// </summary>
        bool IsSuperAdmin { get; }
    }
}
