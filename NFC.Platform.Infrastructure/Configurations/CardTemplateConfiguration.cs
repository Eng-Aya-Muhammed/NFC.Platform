using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NFC.Platform.Domain.Entities;

namespace NFC.Platform.Infrastructure.Configurations
{
    public class CardTemplateConfiguration : IEntityTypeConfiguration<CardTemplate>
    {
        public void Configure(EntityTypeBuilder<CardTemplate> builder)
        {
            builder.ToTable("CardTemplates");
            builder.HasKey(t => t.Id);

            builder.Property(t => t.Name).IsRequired().HasMaxLength(150);
            builder.Property(t => t.Category).IsRequired().HasMaxLength(100);
            builder.Property(t => t.ThumbnailUrl).HasMaxLength(1000);
            builder.Property(t => t.StyleConfigJson).IsRequired().HasColumnType("nvarchar(max)");
        }
    }
}
