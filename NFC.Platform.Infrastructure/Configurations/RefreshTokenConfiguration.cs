using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NFC.Platform.Domain.Entities;

namespace NFC.Platform.Infrastructure.Configurations
{
    public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.ToTable("RefreshTokens");
            builder.HasKey(t => t.Id);

            builder.Property(t => t.Token).IsRequired().HasMaxLength(500);
            builder.Property(t => t.ExpiresOn).IsRequired();

            builder.Ignore(t => t.IsExpired);
            builder.Ignore(t => t.IsActive);

            builder.Property(t => t.TenantId).IsRequired();
            builder.HasIndex(t => new { t.TenantId, t.UserId });

            builder.HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
