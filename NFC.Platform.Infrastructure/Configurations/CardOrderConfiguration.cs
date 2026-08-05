using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NFC.Platform.Domain.Entities;

namespace NFC.Platform.Infrastructure.Configurations
{
    public class CardOrderConfiguration : IEntityTypeConfiguration<CardOrder>
    {
        public void Configure(EntityTypeBuilder<CardOrder> builder)
        {
            builder.ToTable("CardOrders");
            builder.HasKey(o => o.Id);

            builder.Property(o => o.CardDesignId).IsRequired(false);
            builder.HasOne(o => o.CardDesign)
                .WithMany(d => d.Orders)
                .HasForeignKey(o => o.CardDesignId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Property(o => o.Quantity).IsRequired();
            builder.Property(o => o.QuantityPerEmployee).IsRequired().HasDefaultValue(1);

            builder.Property(o => o.Notes).HasMaxLength(2000);
            builder.Property(o => o.Status).IsRequired();
            builder.Property(o => o.TotalPrice).HasColumnType("decimal(18,2)").IsRequired();
            builder.Property(o => o.UnitPrice).HasColumnType("decimal(18,3)").IsRequired();
            builder.Property(o => o.Currency).HasMaxLength(10).IsRequired();
            builder.Property(o => o.TrackingNumber).HasMaxLength(100);
            builder.Property(o => o.DeliveryOtpHash).HasMaxLength(128);

            builder.Property(o => o.TenantId).IsRequired();
            builder.HasIndex(o => o.TenantId);
            builder.HasIndex(o => new { o.TenantId, o.Status, o.CreatedAt });
            builder.HasIndex(o => new { o.CardDesignId, o.Status });

            builder.HasOne(o => o.Tenant)
                .WithMany()
                .HasForeignKey(o => o.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(o => o.User)
                .WithMany()
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
