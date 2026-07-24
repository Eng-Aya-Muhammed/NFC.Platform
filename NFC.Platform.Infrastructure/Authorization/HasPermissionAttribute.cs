using Microsoft.AspNetCore.Authorization;

namespace NFC.Platform.Infrastructure.Authorization
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
    public sealed class HasPermissionAttribute(string permission)
        : AuthorizeAttribute(policy: $"Permission:{permission}");
}
