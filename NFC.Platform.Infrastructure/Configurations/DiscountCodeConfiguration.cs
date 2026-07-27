using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NFC.Platform.Domain.Entities;

namespace NFC.Platform.Infrastructure.Configurations
{
    public class DiscountCodeConfiguration : IEntityTypeConfiguration<DiscountCode>
    {
        public void Configure(EntityTypeBuilder<DiscountCode> builder)
        {
            builder.ToTable("DiscountCodes");
            builder.HasKey(c => c.Id);

            builder.Property(c => c.Code)
                .IsRequired()
                .HasMaxLength(50);

            builder.HasIndex(c => c.Code)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            builder.Property(c => c.DiscountValue)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(c => c.StartDate)
                .IsRequired();

            builder.Property(c => c.EndDate)
                .IsRequired();
        }
    }
}
