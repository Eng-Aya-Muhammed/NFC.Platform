using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NFC.Platform.BuildingBlocks.Common.Seeders;
using NFC.Platform.Domain.Entities;
using NFC.Platform.Domain.Enums;
using NFC.Platform.Infrastructure.Contexts;

namespace NFC.Platform.Infrastructure.Seeders
{
    public class RoleSeeder(ApplicationDbContext context) : IRoleSeeder
    {
        private readonly ApplicationDbContext _context = context ?? throw new ArgumentNullException(nameof(context));

        public async Task SeedAsync()
        {
            var roles = Enum.GetNames<AppRole>();
            var existingRolesDb = await _context.Roles
                .Where(r => roles.Contains(r.Name))
                .ToListAsync();

            var existingRoleNames = existingRolesDb.Select(r => r.Name).ToList();

            foreach (var roleName in roles)
            {
                if (!existingRoleNames.Contains(roleName))
                {
                    _context.Roles.Add(new Role
                    {
                        Name = roleName,
                        IsSystemRole = true,
                        TenantId = null
                    });
                }
                else
                {
                    var existingRole = existingRolesDb.First(r => r.Name == roleName);
                    if (!existingRole.IsSystemRole || existingRole.TenantId != null)
                    {
                        existingRole.IsSystemRole = true;
                        existingRole.TenantId = null;
                        _context.Roles.Update(existingRole);
                    }
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}
