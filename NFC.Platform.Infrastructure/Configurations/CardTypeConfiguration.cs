using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NFC.Platform.Domain.Entities;

namespace NFC.Platform.Infrastructure.Configurations
{
    public class CardTypeConfiguration : IEntityTypeConfiguration<CardType>
    {
        public void Configure(EntityTypeBuilder<CardType> builder)
        {
            builder.ToTable("CardTypes");
            builder.HasKey(ct => ct.Id);

            builder.Property(ct => ct.NameAr).IsRequired().HasMaxLength(200);
            builder.Property(ct => ct.NameEn).IsRequired().HasMaxLength(200);
            builder.Property(ct => ct.PhotoUrl).HasMaxLength(1000);
            builder.Property(ct => ct.IsActive).IsRequired();
        }
    }
}
