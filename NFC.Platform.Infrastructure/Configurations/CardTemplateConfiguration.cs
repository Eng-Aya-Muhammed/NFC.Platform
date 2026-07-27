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

            builder.Property(t => t.NameAr).IsRequired().HasMaxLength(200);
            builder.Property(t => t.NameEn).IsRequired().HasMaxLength(200);
            builder.Property(t => t.PhotoUrl).HasMaxLength(1000);
            builder.Property(t => t.FileUrl).HasMaxLength(1000);

            builder.Property(t => t.CategoryId).IsRequired();
            builder.HasOne(t => t.Category)
                .WithMany()
                .HasForeignKey(t => t.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Property(t => t.IsActive).IsRequired();
            builder.Property(t => t.DisplayOrder).HasDefaultValue(0);
        }
    }
}
