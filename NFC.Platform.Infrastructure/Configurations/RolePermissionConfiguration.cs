using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NFC.Platform.Domain.Entities;

namespace NFC.Platform.Infrastructure.Configurations
{
    public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
    {
        public void Configure(EntityTypeBuilder<RolePermission> builder)
        {
            builder.ToTable("RolePermissions");
            builder.HasKey(rp => rp.Id);

            builder.Property(rp => rp.Permission)
                   .IsRequired()
                   .HasMaxLength(200);

            builder.HasIndex(rp => new { rp.RoleId, rp.Permission }).IsUnique();
        }
    }
}
