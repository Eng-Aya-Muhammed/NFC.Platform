using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NFC.Platform.Domain.Entities;

namespace NFC.Platform.Infrastructure.Configurations
{
    public class SubscriptionPlanTemplateConfiguration : IEntityTypeConfiguration<SubscriptionPlanTemplate>
    {
        public void Configure(EntityTypeBuilder<SubscriptionPlanTemplate> builder)
        {
            builder.ToTable("SubscriptionPlanTemplates");
            builder.HasKey(x => x.Id);

            // Prevent assigning the same template to the same plan twice
            builder.HasIndex(x => new { x.SubscriptionPlanId, x.CardTemplateId }).IsUnique();

            builder.HasOne(x => x.SubscriptionPlan)
                .WithMany(p => p.PlanTemplates)
                .HasForeignKey(x => x.SubscriptionPlanId)
                .OnDelete(DeleteBehavior.Cascade);   // plan deleted → its template mappings go too

            builder.HasOne(x => x.CardTemplate)
                .WithMany(t => t.PlanTemplates)
                .HasForeignKey(x => x.CardTemplateId)
                .OnDelete(DeleteBehavior.Restrict);  // template delete is handled in application layer
        }
    }
}
