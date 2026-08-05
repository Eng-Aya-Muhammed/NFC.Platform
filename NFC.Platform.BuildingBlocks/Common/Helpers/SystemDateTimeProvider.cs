using System;

namespace NFC.Platform.BuildingBlocks.Common.Helpers
{
    public class SystemDateTimeProvider : IDateTimeProvider
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
