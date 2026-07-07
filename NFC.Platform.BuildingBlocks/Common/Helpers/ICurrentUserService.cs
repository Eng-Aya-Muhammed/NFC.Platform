using System;
using System.Collections.Generic;

namespace NFC.Platform.BuildingBlocks.Common.Helpers
{
    /// <summary>
    /// Service contract to retrieve identity information about the currently logged-in user.
    /// </summary>
    public interface ICurrentUserService
    {
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
        /// Gets the list of roles assigned to the current user.
        /// </summary>
        IEnumerable<string> Roles { get; }
    }
}
