using System;

namespace NFC.Platform.BuildingBlocks.Common.Helpers
{
    /// <summary>
    /// Default system implementation of <see cref="IDateTimeProvider"/> returning standard machine UTC time.
    /// </summary>
    public class SystemDateTimeProvider : IDateTimeProvider
    {
        /// <inheritdoc />
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
