using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NFC.Platform.BuildingBlocks.Common.Seeders;
using NFC.Platform.Domain.Entities;
using NFC.Platform.Infrastructure.Contexts;

namespace NFC.Platform.Infrastructure.Seeders
{
    /// <summary>
    /// Seeds the system-wide default CardTemplate (TenantId = null) used as a fallback
    /// when neither a Company nor a UserProfile has selected a template.
    /// Also normalizes StyleConfigJson on all existing templates to the canonical shape:
    ///   { "layout": "...", "primaryColor": "...", "secondaryColor": "..." }
    /// </summary>
    public class DefaultCardTemplateSeeder(
        ApplicationDbContext context,
        ILogger<DefaultCardTemplateSeeder> logger) : IDefaultCardTemplateSeeder
    {
        private readonly ApplicationDbContext _context = context ?? throw new ArgumentNullException(nameof(context));
        private readonly ILogger<DefaultCardTemplateSeeder> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        private const string DefaultTemplateName = "Default";
        private const string DefaultStyleConfigJson = """{"layout":"classic","primaryColor":"#1A1AFF","secondaryColor":"#F5F5F5"}""";

        public async Task SeedAsync()
        {
            // 1. Seed the global default template if it doesn't exist
            var exists = await _context.CardTemplates
                .IgnoreQueryFilters()
                .AnyAsync(t => t.TenantId == null && t.Name == DefaultTemplateName);

            if (!exists)
            {
                _context.CardTemplates.Add(new CardTemplate
                {
                    Name = DefaultTemplateName,
                    Category = "System",
                    Description = "System default profile layout — applied when no custom template is selected.",
                    ThumbnailUrl = string.Empty,
                    StyleConfigJson = DefaultStyleConfigJson,
                    IsActive = true,
                    DisplayOrder = 0,
                    TenantId = null
                });

                await _context.SaveChangesAsync();
                _logger.LogInformation("[DefaultCardTemplateSeeder] Default CardTemplate seeded successfully.");
            }

            // 2. Normalize StyleConfigJson on all existing templates
            await NormalizeExistingTemplatesAsync();
        }

        private async Task NormalizeExistingTemplatesAsync()
        {
            var templates = await _context.CardTemplates
                .IgnoreQueryFilters()
                .ToListAsync();

            var unmappable = new List<Guid>();

            foreach (var template in templates)
            {
                if (string.IsNullOrWhiteSpace(template.StyleConfigJson))
                {
                    template.StyleConfigJson = DefaultStyleConfigJson;
                    continue;
                }

                try
                {
                    using var doc = JsonDocument.Parse(template.StyleConfigJson);
                    var root = doc.RootElement;

                    // Check if it already has the canonical shape
                    var hasLayout = root.TryGetProperty("layout", out _);
                    var hasPrimary = root.TryGetProperty("primaryColor", out _);
                    var hasSecondary = root.TryGetProperty("secondaryColor", out _);

                    if (hasLayout && hasPrimary && hasSecondary)
                        continue; // Already canonical — leave as-is

                    // Best-effort mapping from alternative field names
                    var layout = TryGetStringValue(root, "layout", "template", "layoutId", "type") ?? "classic";
                    var primaryColor = TryGetStringValue(root, "primaryColor", "primary", "color", "mainColor", "brandColor") ?? "#1A1AFF";
                    var secondaryColor = TryGetStringValue(root, "secondaryColor", "secondary", "accentColor", "backgroundColor", "bgColor") ?? "#F5F5F5";

                    var mapped = JsonSerializer.Serialize(new
                    {
                        layout,
                        primaryColor,
                        secondaryColor
                    });

                    // If nothing was mappable (all came from defaults), flag for manual review
                    var allDefaults = layout == "classic" && primaryColor == "#1A1AFF" && secondaryColor == "#F5F5F5";
                    if (allDefaults && !hasLayout && !hasPrimary && !hasSecondary)
                    {
                        unmappable.Add(template.Id);
                    }

                    template.StyleConfigJson = mapped;
                }
                catch (JsonException)
                {
                    // Couldn't parse at all — flag and reset to default
                    unmappable.Add(template.Id);
                    template.StyleConfigJson = DefaultStyleConfigJson;
                }
            }

            if (unmappable.Count > 0)
            {
                _logger.LogWarning(
                    "[DefaultCardTemplateSeeder] StyleConfigJson normalization WARNING: " +
                    "The following CardTemplate Ids could not be reasonably mapped and were reset to defaults. " +
                    "Review and update them manually: [{Ids}]",
                    string.Join(", ", unmappable));
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("[DefaultCardTemplateSeeder] StyleConfigJson normalization complete. {Count} template(s) processed.", templates.Count);
        }

        private static string? TryGetStringValue(JsonElement root, params string[] candidateKeys)
        {
            foreach (var key in candidateKeys)
            {
                if (root.TryGetProperty(key, out var prop) && prop.ValueKind == JsonValueKind.String)
                {
                    var val = prop.GetString();
                    if (!string.IsNullOrWhiteSpace(val))
                        return val;
                }
            }

            return null;
        }
    }
}
