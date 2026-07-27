using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NFC.Platform.BuildingBlocks.Common.Seeders;
using NFC.Platform.Domain.Entities;
using NFC.Platform.Infrastructure.Contexts;

namespace NFC.Platform.Infrastructure.Seeders
{
    public class DefaultCardTemplateSeeder(
        ApplicationDbContext context,
        ILogger<DefaultCardTemplateSeeder> logger) : IDefaultCardTemplateSeeder
    {
        private readonly ApplicationDbContext _context = context ?? throw new ArgumentNullException(nameof(context));
        private readonly ILogger<DefaultCardTemplateSeeder> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        private const string DefaultTemplateNameAr = "افتراضي";
        private const string DefaultTemplateNameEn = "Default";

        public async Task SeedAsync()
        {
            var defaultCategory = await _context.TemplateCategories
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.NameEn == "System" || c.NameAr == "نظام");

            if (defaultCategory == null)
            {
                defaultCategory = new TemplateCategory
                {
                    NameAr = "نظام",
                    NameEn = "System",
                    IsActive = true,
                    DisplayOrder = 0
                };
                _context.TemplateCategories.Add(defaultCategory);
                await _context.SaveChangesAsync();
            }

            var exists = await _context.CardTemplates
                .IgnoreQueryFilters()
                .AnyAsync(t => t.NameEn == DefaultTemplateNameEn || t.NameAr == DefaultTemplateNameAr);

            if (!exists)
            {
                _context.CardTemplates.Add(new CardTemplate
                {
                    NameAr = DefaultTemplateNameAr,
                    NameEn = DefaultTemplateNameEn,
                    CategoryId = defaultCategory.Id,
                    IsActive = true,
                    DisplayOrder = 0
                });

                await _context.SaveChangesAsync();
                _logger.LogInformation("[DefaultCardTemplateSeeder] Default CardTemplate seeded successfully.");
            }
        }
    }
}
