using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NFC.Platform.Domain.Entities;

namespace NFC.Platform.Infrastructure.Configurations
{
    public class CompanyConfiguration : IEntityTypeConfiguration<Company>
    {
        public void Configure(EntityTypeBuilder<Company> builder)
        {
            builder.ToTable("Companies");
            builder.HasKey(c => c.Id);

            builder.Property(c => c.Name).IsRequired().HasMaxLength(256);
            builder.Property(c => c.Activity).HasMaxLength(256);
            builder.Property(c => c.CommercialRegistry).HasMaxLength(100);
            builder.Property(c => c.Size).HasMaxLength(50);
            builder.Property(c => c.Address).HasMaxLength(500);

            builder.Property(c => c.TenantId).IsRequired();
            builder.HasIndex(c => c.TenantId).IsUnique();

            builder.HasOne(c => c.AdminUser)
                .WithMany()
                .HasForeignKey(c => c.AdminUserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(c => c.ProfileTemplate)
                .WithMany()
                .HasForeignKey(c => c.ProfileTemplateId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
