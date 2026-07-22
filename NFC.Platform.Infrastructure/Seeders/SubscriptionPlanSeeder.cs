using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NFC.Platform.BuildingBlocks.Common.Seeders;
using NFC.Platform.Domain.Entities;
using NFC.Platform.Infrastructure.Contexts;

namespace NFC.Platform.Infrastructure.Seeders
{
    public class SubscriptionPlanSeeder(ApplicationDbContext context) : ISubscriptionPlanSeeder
    {
        private readonly ApplicationDbContext _context = context ?? throw new ArgumentNullException(nameof(context));

        public async Task SeedAsync()
        {
            var plans = new[]
            {
                new SubscriptionPlan
                {
                    Name = "PremiumAnnual",
                    Description = "PremiumDescription",
                    Price = 699.00m,
                    DurationInDays = 365,
                    MaxEmployees = 100
                },
                new SubscriptionPlan
                {
                    Name = "Premium3Years",
                    Description = "PremiumDescription",
                    Price = 699.00m,
                    DurationInDays = 1095,
                    MaxEmployees = 100
                },
                new SubscriptionPlan
                {
                    Name = "Premium5Years",
                    Description = "PremiumDescription",
                    Price = 699.00m,
                    DurationInDays = 1825,
                    MaxEmployees = 100
                }
            };

            var planNames = plans.Select(p => p.Name).ToList();
            var existingPlans = await _context.SubscriptionPlans
                .Where(p => planNames.Contains(p.Name))
                .Select(p => p.Name)
                .ToListAsync();

            foreach (var plan in plans)
            {
                if (!existingPlans.Contains(plan.Name))
                {
                    _context.SubscriptionPlans.Add(plan);
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}
