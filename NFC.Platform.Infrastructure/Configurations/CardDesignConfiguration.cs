using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NFC.Platform.Domain.Entities;

namespace NFC.Platform.Infrastructure.Configurations
{
    public class CardDesignConfiguration : IEntityTypeConfiguration<CardDesign>
    {
        public void Configure(EntityTypeBuilder<CardDesign> builder)
        {
            builder.ToTable("CardDesigns");
            builder.HasKey(d => d.Id);

            builder.Property(d => d.TotalQuantity).IsRequired();
            builder.Property(d => d.UsedQuantity).IsRequired().HasDefaultValue(0);
            builder.Property(d => d.UnitPrice).HasColumnType("decimal(18,3)").IsRequired();
            builder.Property(d => d.TotalPrice).HasColumnType("decimal(18,2)").IsRequired();
            builder.Property(d => d.Currency).HasMaxLength(3).IsRequired();

            builder.Property(d => d.ExcelDataUrl).HasMaxLength(1000);
            builder.Property(d => d.FrontDesignUrl).HasMaxLength(1000);
            builder.Property(d => d.BackDesignUrl).HasMaxLength(1000);
            builder.Property(d => d.CardDesignType).IsRequired();

            builder.Property(d => d.IsPaid).IsRequired().HasDefaultValue(false);
            builder.Property(d => d.PaymentStatus).IsRequired();
            builder.Property(d => d.PaidAt);
            builder.Property(d => d.PaymentTransactionId).HasMaxLength(500);

            builder.Property(d => d.Notes).HasMaxLength(2000);

            builder.Property(d => d.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();

            builder.Property(d => d.TenantId).IsRequired();
            builder.HasIndex(d => d.TenantId);

            builder.HasOne(d => d.Tenant)
                .WithMany()
                .HasForeignKey(d => d.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Property(d => d.CardTypeId).IsRequired();
            builder.HasOne(d => d.CardType)
                .WithMany()
                .HasForeignKey(d => d.CardTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Property(d => d.CardPackageId).IsRequired();
            builder.HasOne(d => d.CardPackage)
                .WithMany()
                .HasForeignKey(d => d.CardPackageId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(d => d.Orders)
                .WithOne(o => o.CardDesign)
                .HasForeignKey(o => o.CardDesignId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
