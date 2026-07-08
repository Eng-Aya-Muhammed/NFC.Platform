using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NFC.Platform.Domain.Entities;

namespace NFC.Platform.Infrastructure.Configurations
{
    public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
    {
        public void Configure(EntityTypeBuilder<Tenant> builder)
        {
            builder.ToTable("Tenants");
            builder.HasKey(t => t.Id);

            builder.Property(t => t.Name)
                .IsRequired()
                .HasMaxLength(256);

            builder.Property(t => t.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            // One-to-One: Tenant <-> Company (Company has TenantId)
            builder.HasOne(t => t.Company)
                .WithOne(c => c.Tenant)
                .HasForeignKey<Company>(c => c.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            // One-to-Many: Tenant -> Users
            builder.HasMany(t => t.Users)
                .WithOne(u => u.Tenant)
                .HasForeignKey(u => u.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
