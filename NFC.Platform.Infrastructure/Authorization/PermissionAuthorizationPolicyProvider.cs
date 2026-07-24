using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace NFC.Platform.Infrastructure.Authorization
{
    public class PermissionAuthorizationPolicyProvider(IOptions<AuthorizationOptions> options)
        : DefaultAuthorizationPolicyProvider(options)
    {
        private const string PermissionPrefix = "Permission:";

        public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
        {
            if (policyName.StartsWith(PermissionPrefix, System.StringComparison.OrdinalIgnoreCase))
            {
                var permission = policyName[PermissionPrefix.Length..];

                var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .AddRequirements(new PermissionRequirement(permission))
                    .Build();

                return policy;
            }

            return await base.GetPolicyAsync(policyName);
        }
    }
}
