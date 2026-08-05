using System;
using System.Collections.Generic;

namespace NFC.Platform.Application.Interfaces.Services
{
    public interface ITokenService
    {
        string GenerateToken(Guid userId, string email, IEnumerable<string> roles, Guid tenantId, Guid? companyId = null, string? accountType = null);

    }
}
