using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NFC.Platform.Domain.Entities;

namespace NFC.Platform.Infrastructure.Configurations
{
    public class TemplateRequestConfiguration : IEntityTypeConfiguration<TemplateRequest>
    {
        public void Configure(EntityTypeBuilder<TemplateRequest> builder)
        {
            builder.ToTable("TemplateRequests");
            builder.HasKey(r => r.Id);

            builder.Property(r => r.TemplateName).IsRequired().HasMaxLength(150);
            builder.Property(r => r.LogoUrl).HasMaxLength(1000);
            builder.Property(r => r.ReferenceImageUrl).HasMaxLength(1000);
            builder.Property(r => r.Notes).HasMaxLength(1000);
            builder.Property(r => r.Status).IsRequired();

            builder.Property(r => r.TenantId).IsRequired();
            builder.HasIndex(r => r.TenantId);

            builder.HasOne(r => r.Tenant)
                .WithMany()
                .HasForeignKey(r => r.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(r => r.RequestedByUser)
                .WithMany()
                .HasForeignKey(r => r.RequestedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
