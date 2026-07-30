using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NFC.Platform.BuildingBlocks.Common.Seeders;
using NFC.Platform.Domain.Constants;
using NFC.Platform.Domain.Entities;
using NFC.Platform.Domain.Enums;
using NFC.Platform.Infrastructure.Contexts;

namespace NFC.Platform.Infrastructure.Seeders
{
    public class PermissionSeeder(ApplicationDbContext context) : IPermissionSeeder
    {
        private readonly ApplicationDbContext _context = context ?? throw new ArgumentNullException(nameof(context));

        private static readonly Dictionary<AppRole, IReadOnlyList<string>> DefaultPermissions = new()
        {
            [AppRole.CompanyAdmin] = AppPermissions.GetTenantPermissions().ToList(),

            [AppRole.Customer] =
            [
                // Profiles
                AppPermissions.Profiles.View,
                AppPermissions.Profiles.Update,

                // Subscriptions
                AppPermissions.Subscriptions.View,
                AppPermissions.Subscriptions.Update,

                // Card Designs
                AppPermissions.CardDesigns.View,
                AppPermissions.CardDesigns.Create,
                AppPermissions.CardDesigns.Update,
                AppPermissions.CardDesigns.Delete,

                // Card Orders
                AppPermissions.CardOrders.View,
                AppPermissions.CardOrders.Create,
                AppPermissions.CardOrders.Update,
                AppPermissions.CardOrders.Cancel,

                // Template Requests
                AppPermissions.TemplateRequests.View,
                AppPermissions.TemplateRequests.Create,
                AppPermissions.TemplateRequests.Update,
                AppPermissions.TemplateRequests.Cancel,

                // Analytics
                AppPermissions.Analytics.View,

                // No Employees (individual user)
            ],
        };

        public async Task SeedAsync()
        {
            foreach (var (appRole, permissions) in DefaultPermissions)
            {
                var roleName = appRole.ToString();

                var role = await _context.Roles
                    .FirstOrDefaultAsync(r => r.Name == roleName && r.IsSystemRole);

                if (role is null) continue;

                var existingPermissions = await _context.RolePermissions
                    .Where(rp => rp.RoleId == role.Id)
                    .Select(rp => rp.Permission)
                    .ToListAsync();

                foreach (var permission in permissions)
                {
                    if (!existingPermissions.Contains(permission))
                    {
                        _context.RolePermissions.Add(new RolePermission
                        {
                            RoleId = role.Id,
                            Permission = permission
                        });
                    }
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}
