using Microsoft.AspNetCore.Authorization;

namespace NFC.Platform.Infrastructure.Authorization
{
    public record PermissionRequirement(string Permission) : IAuthorizationRequirement;
}
