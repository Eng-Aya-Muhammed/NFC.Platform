using System;
using System.Collections.Generic;

namespace NFC.Platform.Application.Interfaces.Services
{
    /// <summary>
    /// Service contract for security token operations, such as signing JWTs.
    /// </summary>
    public interface ITokenService
    {
        /// <summary>
        /// Generates a signed JSON Web Token (JWT) containing the user identity, email, and roles.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="email">The email address of the user.</param>
        /// <param name="roles">The list of roles assigned to the user.</param>
        /// <returns>A signed JWT token string.</returns>
        string GenerateToken(Guid userId, string email, IEnumerable<string> roles, Guid? companyId = null, string? accountType = null);

    }
}
