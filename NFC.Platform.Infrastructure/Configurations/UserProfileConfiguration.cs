using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NFC.Platform.Domain.Entities;

namespace NFC.Platform.Infrastructure.Configurations
{
    public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
    {
        public void Configure(EntityTypeBuilder<UserProfile> builder)
        {
            builder.ToTable("UserProfiles");
            builder.HasKey(p => p.Id);

            builder.Property(p => p.FullName).IsRequired().HasMaxLength(256);
            builder.Property(p => p.JobTitle).HasMaxLength(256);
            builder.Property(p => p.Department).HasMaxLength(150);
            builder.Property(p => p.CompanyName).HasMaxLength(256);
            builder.Property(p => p.ProfilePictureUrl).HasMaxLength(1000);
            builder.Property(p => p.Bio).HasMaxLength(500);

            builder.Property(p => p.ContactEmail).HasMaxLength(256);
            builder.Property(p => p.Phone).HasMaxLength(50);
            builder.Property(p => p.WhatsApp).HasMaxLength(50);

            builder.Property(p => p.InstagramUrl).HasMaxLength(1000);
            builder.Property(p => p.FacebookUrl).HasMaxLength(1000);
            builder.Property(p => p.LinkedInUrl).HasMaxLength(1000);
            builder.Property(p => p.WebsiteUrl).HasMaxLength(1000);

            builder.Property(p => p.TenantId).IsRequired();
            builder.HasIndex(p => p.TenantId);

            builder.HasIndex(p => p.UserId)
                .IsUnique()
                .HasFilter("[UserId] IS NOT NULL");

            builder.HasIndex(p => p.EmployeeId)
                .IsUnique()
                .HasFilter("[EmployeeId] IS NOT NULL");

            builder.HasOne(p => p.Tenant)
                .WithMany()
                .HasForeignKey(p => p.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(p => p.CardTemplate)
                .WithMany()
                .HasForeignKey(p => p.CardTemplateId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
