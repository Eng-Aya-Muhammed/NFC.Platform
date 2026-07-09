using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NFC.Platform.Domain.Entities;

namespace NFC.Platform.Infrastructure.Configurations
{
    public class CardOrderItemConfiguration : IEntityTypeConfiguration<CardOrderItem>
    {
        public void Configure(EntityTypeBuilder<CardOrderItem> builder)
        {
            builder.ToTable("CardOrderItems");
            builder.HasKey(i => i.Id);

            builder.Property(i => i.EmployeeName)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(i => i.JobTitle).HasMaxLength(100);
            builder.Property(i => i.Email).HasMaxLength(256);
            builder.Property(i => i.Phone).HasMaxLength(50);
            builder.Property(i => i.Department).HasMaxLength(100);
            builder.Property(i => i.ActivationCode).HasMaxLength(100);

            builder.Property(i => i.TenantId).IsRequired();
            builder.HasIndex(i => i.TenantId);
            builder.HasIndex(i => i.ActivationCode);

            builder.HasOne(i => i.Tenant)
                .WithMany()
                .HasForeignKey(i => i.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(i => i.CardOrder)
                .WithMany(o => o.Items)
                .HasForeignKey(i => i.CardOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(i => i.LinkedCard)
                .WithMany()
                .HasForeignKey(i => i.LinkedCardId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
