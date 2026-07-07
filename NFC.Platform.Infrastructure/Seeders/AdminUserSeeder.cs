using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NFC.Platform.BuildingBlocks.Common.Helpers;
using NFC.Platform.BuildingBlocks.Common.Seeders;
using NFC.Platform.Domain.Entities;
using NFC.Platform.Domain.Enums;
using NFC.Platform.Infrastructure.Contexts;

namespace NFC.Platform.Infrastructure.Seeders
{
    public class AdminUserSeeder : IAdminUserSeeder
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public AdminUserSeeder(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public async Task SeedAsync()
        {
            var email = _configuration["AdminSettings:Email"];
            var password = _configuration["AdminSettings:Password"];
            var username = _configuration["AdminSettings:Username"];

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                return;
            }

            var adminExists = await _context.Users.AnyAsync(u => u.Email == email);
            if (!adminExists)
            {
                var passwordHash = PasswordHasher.HashPassword(password);
                var adminUser = new User
                {
                    Username = username ?? "admin",
                    Email = email,
                    PasswordHash = passwordHash
                };

                _context.Users.Add(adminUser);
                await _context.SaveChangesAsync();

                var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == AppRole.Admin.ToString());
                if (adminRole != null)
                {
                    _context.UserRoles.Add(new UserRole
                    {
                        UserId = adminUser.Id,
                        RoleId = adminRole.Id
                    });
                    await _context.SaveChangesAsync();
                }
            }
        }
    }
}
