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
            var existingRoles = await _context.Roles
                .Where(r => roles.Contains(r.Name))
                .Select(r => r.Name)
                .ToListAsync();

            foreach (var roleName in roles)
            {
                if (!existingRoles.Contains(roleName))
                {
                    _context.Roles.Add(new Role { Name = roleName });
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}
