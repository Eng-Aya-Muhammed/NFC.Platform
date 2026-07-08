using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NFC.Platform.Domain.Entities;

namespace NFC.Platform.Infrastructure.Configurations
{
    public class CardConfiguration : IEntityTypeConfiguration<Card>
    {
        public void Configure(EntityTypeBuilder<Card> builder)
        {
            builder.ToTable("Cards");
            builder.HasKey(c => c.Id);

            builder.Property(c => c.TenantId).IsRequired();

            builder.Property(c => c.ActivationCode)
                .IsRequired()
                .HasMaxLength(100);

            builder.HasIndex(c => new { c.TenantId, c.ActivationCode }).IsUnique();

            builder.HasOne(c => c.Tenant)
                .WithMany()
                .HasForeignKey(c => c.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(c => c.UserProfile)
                .WithMany(p => p.ActivatedCards)
                .HasForeignKey(c => c.UserProfileId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(c => c.CardOrder)
                .WithMany(o => o.GeneratedCards)
                .HasForeignKey(c => c.CardOrderId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
