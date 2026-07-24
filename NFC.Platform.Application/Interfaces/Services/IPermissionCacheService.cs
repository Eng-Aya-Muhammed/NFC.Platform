using System;
using System.Threading.Tasks;

namespace NFC.Platform.Application.Interfaces.Services
{
    public interface IPermissionCacheService
    {
        void InvalidateUser(Guid userId);

        Task InvalidateRoleUsersAsync(Guid roleId);
    }
}
