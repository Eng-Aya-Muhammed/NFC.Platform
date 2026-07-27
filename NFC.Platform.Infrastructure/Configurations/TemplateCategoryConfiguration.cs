using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NFC.Platform.Domain.Entities;

namespace NFC.Platform.Infrastructure.Configurations
{
    public class TemplateCategoryConfiguration : IEntityTypeConfiguration<TemplateCategory>
    {
        public void Configure(EntityTypeBuilder<TemplateCategory> builder)
        {
            builder.ToTable("TemplateCategories");
            builder.HasKey(c => c.Id);

            builder.Property(c => c.NameAr).IsRequired().HasMaxLength(200);
            builder.Property(c => c.NameEn).IsRequired().HasMaxLength(200);
            builder.Property(c => c.IsActive).IsRequired();
            builder.Property(c => c.DisplayOrder).HasDefaultValue(0);
        }
    }
}
