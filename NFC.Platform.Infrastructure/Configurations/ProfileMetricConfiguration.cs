using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NFC.Platform.Domain.Entities;

namespace NFC.Platform.Infrastructure.Configurations
{
    public class ProfileMetricConfiguration : IEntityTypeConfiguration<ProfileMetric>
    {
        public void Configure(EntityTypeBuilder<ProfileMetric> builder)
        {
            builder.ToTable("ProfileMetrics");
            builder.HasKey(m => m.Id);

            builder.Property(m => m.InteractionType).IsRequired();

            builder.Property(m => m.TenantId).IsRequired();
            builder.HasIndex(m => m.TenantId);

            builder.HasOne(m => m.Tenant)
                .WithMany()
                .HasForeignKey(m => m.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(m => m.UserProfile)
                .WithMany()
                .HasForeignKey(m => m.UserProfileId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(m => m.ProfileLink)
                .WithMany()
                .HasForeignKey(m => m.ProfileLinkId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
