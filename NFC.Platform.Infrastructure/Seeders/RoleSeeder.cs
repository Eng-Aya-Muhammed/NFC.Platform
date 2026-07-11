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

            foreach (var roleName in roles)
            {
                var roleExists = await _context.Roles.AnyAsync(r => r.Name == roleName);
                if (!roleExists)
                {
                    _context.Roles.Add(new Role { Name = roleName });
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}
