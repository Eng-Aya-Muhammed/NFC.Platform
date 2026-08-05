using System;

namespace NFC.Platform.BuildingBlocks.Common.Helpers
{
    public interface IDateTimeProvider
    {
        DateTime UtcNow { get; }
    }
}
