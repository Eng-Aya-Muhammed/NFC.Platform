using System;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using NFC.Platform.Domain.Common;
using NFC.Platform.Domain.Entities;
using NFC.Platform.Infrastructure.Interceptors;

namespace NFC.Platform.Infrastructure.Contexts
{
    public class ApplicationDbContext : DbContext
    {
        private readonly AuditableEntitySaveChangesInterceptor _auditableInterceptor;

        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options,
            AuditableEntitySaveChangesInterceptor auditableInterceptor) : base(options)
        {
            _auditableInterceptor = auditableInterceptor ?? throw new ArgumentNullException(nameof(auditableInterceptor));
        }

        public DbSet<Card> Cards { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<ProfileLink> ProfileLinks { get; set; }
        public DbSet<CardTemplate> CardTemplates { get; set; }
        public DbSet<CardOrder> CardOrders { get; set; }
        public DbSet<CardOrderItem> CardOrderItems { get; set; }
        public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
        public DbSet<UserSubscription> UserSubscriptions { get; set; }
        public DbSet<ProfileMetric> ProfileMetrics { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.AddInterceptors(_auditableInterceptor);
            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
                {
                    modelBuilder.Entity(entityType.ClrType).HasQueryFilter(CreateSoftDeleteFilter(entityType.ClrType));
                }
            }
        }

        private static LambdaExpression CreateSoftDeleteFilter(Type entityType)
        {
            var parameter = Expression.Parameter(entityType, "e");
            var property = Expression.Property(parameter, nameof(BaseEntity.IsDeleted));
            var falseValue = Expression.Constant(false);
            var body = Expression.Equal(property, falseValue);
            return Expression.Lambda(body, parameter);
        }
    }
}
