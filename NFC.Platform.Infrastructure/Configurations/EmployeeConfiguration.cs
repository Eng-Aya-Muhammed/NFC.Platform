using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NFC.Platform.Domain.Entities;

namespace NFC.Platform.Infrastructure.Configurations
{
    public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
    {
        public void Configure(EntityTypeBuilder<Employee> builder)
        {
            builder.ToTable("Employees");
            builder.HasKey(e => e.Id);

            builder.Property(e => e.FullName).IsRequired().HasMaxLength(256);
            builder.Property(e => e.Email).IsRequired().HasMaxLength(256);
            builder.Property(e => e.JobTitle).HasMaxLength(256);
            builder.Property(e => e.Department).HasMaxLength(150);
            builder.Property(e => e.Status).HasDefaultValue(Domain.Enums.UserStatus.Active);

            builder.Property(e => e.TenantId).IsRequired();
            builder.HasIndex(e => e.TenantId);

            builder.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.Company)
                .WithMany(c => c.Employees)
                .HasForeignKey(e => e.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.UserProfile)
                .WithOne(p => p.Employee)
                .HasForeignKey<UserProfile>(p => p.EmployeeId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
