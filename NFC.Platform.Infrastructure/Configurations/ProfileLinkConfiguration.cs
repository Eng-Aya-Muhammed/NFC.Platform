using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NFC.Platform.Domain.Entities;

namespace NFC.Platform.Infrastructure.Configurations
{
    public class ProfileLinkConfiguration : IEntityTypeConfiguration<ProfileLink>
    {
        public void Configure(EntityTypeBuilder<ProfileLink> builder)
        {
            builder.ToTable("ProfileLinks");
            builder.HasKey(l => l.Id);

            builder.Property(l => l.Title).IsRequired().HasMaxLength(150);
            builder.Property(l => l.Url).IsRequired().HasMaxLength(1000);
            builder.Property(l => l.DisplayOrder).IsRequired();

            builder.Property(l => l.TenantId).IsRequired();
            builder.HasIndex(l => l.TenantId);

            builder.HasOne(l => l.Tenant)
                .WithMany()
                .HasForeignKey(l => l.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(l => l.UserProfile)
                .WithMany(p => p.CustomLinks)
                .HasForeignKey(l => l.UserProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
