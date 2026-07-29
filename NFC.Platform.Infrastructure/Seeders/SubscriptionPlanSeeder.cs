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
                    NameAr = "الخطة السنوية الممتازة",
                    NameEn = "Premium Annual Plan",
                    Description = "Premium Annual Subscription",
                    Price = 699.00m,
                    DurationInDays = 365
                },
                new SubscriptionPlan
                {
                    NameAr = "خطة الـ 3 سنوات الممتازة",
                    NameEn = "Premium 3-Year Plan",
                    Description = "Premium 3-Year Subscription",
                    Price = 699.00m,
                    DurationInDays = 1095
                },
                new SubscriptionPlan
                {
                    NameAr = "خطة الـ 5 سنوات الممتازة",
                    NameEn = "Premium 5-Year Plan",
                    Description = "Premium 5-Year Subscription",
                    Price = 699.00m,
                    DurationInDays = 1825
                }
            };

            var planNamesAr = plans.Select(p => p.NameAr).ToList();
            var existingPlans = await _context.SubscriptionPlans
                .Where(p => planNamesAr.Contains(p.NameAr))
                .Select(p => p.NameAr)
                .ToListAsync();

            foreach (var plan in plans)
            {
                if (!existingPlans.Contains(plan.NameAr))
                {
                    _context.SubscriptionPlans.Add(plan);
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}
