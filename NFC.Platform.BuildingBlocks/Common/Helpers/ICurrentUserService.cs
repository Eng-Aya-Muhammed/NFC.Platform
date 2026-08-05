using System;
using System.Collections.Generic;

namespace NFC.Platform.BuildingBlocks.Common.Helpers
{
    public interface ICurrentUserService
    {
        Guid? UserId { get; }

        string? Email { get; }

        bool IsAuthenticated { get; }

        IEnumerable<string> Roles { get; }
    }
}
