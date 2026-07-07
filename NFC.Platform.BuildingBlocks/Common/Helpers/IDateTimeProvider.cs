using System;

namespace NFC.Platform.BuildingBlocks.Common.Helpers
{
    /// <summary>
    /// Contract providing date and time retrieval, enabling testability of time-dependent features.
    /// </summary>
    public interface IDateTimeProvider
    {
        /// <summary>
        /// Gets the current date and time in Coordinated Universal Time (UTC).
        /// </summary>
        DateTime UtcNow { get; }
    }
}
