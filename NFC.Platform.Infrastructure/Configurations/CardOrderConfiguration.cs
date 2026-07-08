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

            builder.Property(o => o.CardName).IsRequired().HasMaxLength(200);
            builder.Property(o => o.CardType).IsRequired();
            builder.Property(o => o.CardDesignType).IsRequired();
            builder.Property(o => o.Quantity).IsRequired();

            builder.Property(o => o.ExcelDataUrl).HasMaxLength(1000);
            builder.Property(o => o.FrontDesignUrl).HasMaxLength(1000);
            builder.Property(o => o.BackDesignUrl).HasMaxLength(1000);

            builder.Property(o => o.Notes).HasMaxLength(2000);
            builder.Property(o => o.Status).IsRequired();
            builder.Property(o => o.TotalPrice).HasColumnType("decimal(18,2)").IsRequired();

            builder.Property(o => o.TenantId).IsRequired();
            builder.HasIndex(o => o.TenantId);

            builder.HasOne(o => o.Tenant)
                .WithMany()
                .HasForeignKey(o => o.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(o => o.User)
                .WithMany()
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(o => o.PrintTemplate)
                .WithMany()
                .HasForeignKey(o => o.PrintTemplateId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
