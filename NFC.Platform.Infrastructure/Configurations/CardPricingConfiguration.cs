using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NFC.Platform.Infrastructure.Configurations
{
    public class CardPricingConfiguration : IEntityTypeConfiguration<CardPricing>
    {
        public void Configure(EntityTypeBuilder<CardPricing> builder)
        {
            builder.ToTable("CardPricings");
            builder.HasKey(p => p.Id);
            builder.Property(p => p.UnitPrice).HasColumnType("decimal(18,3)").IsRequired();
            builder.Property(p => p.Currency).IsRequired().HasMaxLength(10);
            builder.Property(p => p.CardType).IsRequired();
            builder.Property(p => p.IsActive).IsRequired();
            builder.Property(p => p.EffectiveFrom).IsRequired();
            builder.HasIndex(p => new { p.CardType, p.IsActive });
        }
    }
}
