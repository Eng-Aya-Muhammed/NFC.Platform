using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NFC.Platform.Domain.Entities;

namespace NFC.Platform.Infrastructure.Configurations
{
    public class CardPackageConfiguration : IEntityTypeConfiguration<CardPackage>
    {
        public void Configure(EntityTypeBuilder<CardPackage> builder)
        {
            builder.ToTable("CardPackages");
            builder.HasKey(cp => cp.Id);

            builder.Property(cp => cp.NumberOfCards).IsRequired();
            builder.Property(cp => cp.Price).HasColumnType("decimal(18,3)").IsRequired();
            builder.Property(cp => cp.IsActive).IsRequired();
        }
    }
}
