using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NFC.Platform.BuildingBlocks.Common.Helpers;
using NFC.Platform.Domain.Common;
using NFC.Platform.Domain.Entities;
using NFC.Platform.Infrastructure.Interceptors;

namespace NFC.Platform.Infrastructure.Contexts
{
    public class ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        AuditableEntitySaveChangesInterceptor auditableInterceptor,
        ICurrentTenant currentTenant) : DbContext(options)
    {
        private readonly AuditableEntitySaveChangesInterceptor _auditableInterceptor = auditableInterceptor ?? throw new ArgumentNullException(nameof(auditableInterceptor));
        private readonly ICurrentTenant _currentTenant = currentTenant ?? throw new ArgumentNullException(nameof(currentTenant));

        public Guid CurrentTenantId => _currentTenant.TenantId ?? Guid.Empty;
        public bool IsSuperAdmin => _currentTenant.IsSuperAdmin;

        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<Card> Cards { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Employee> Employees { get; set; }
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
        public DbSet<TemplateRequest> TemplateRequests { get; set; }
        public DbSet<EmployeeImportJob> EmployeeImportJobs { get; set; }
        public DbSet<CardPricing> CardPricings { get; set; }


        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.AddInterceptors(_auditableInterceptor);
            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

            modelBuilder.Entity<EmployeeImportJob>(builder =>
            {
                builder.ToTable("EmployeeImportJobs");
                builder.HasKey(j => j.Id);
                builder.Property(j => j.FileName).IsRequired().HasMaxLength(200);
                builder.Property(j => j.ExcelFileUrl).IsRequired().HasMaxLength(1000);
                builder.Property(j => j.ExcelFilePublicId).HasMaxLength(500);
                builder.Property(j => j.Status).IsRequired();
                builder.Property(j => j.Notes).HasMaxLength(2000);
                builder.Property(j => j.DesignReferenceUrl).HasMaxLength(1000);
                builder.Property(j => j.LogoUrl).HasMaxLength(1000);
                builder.Property(j => j.DesignNotes).HasMaxLength(2000);
                builder.Property(j => j.TenantId).IsRequired();
                builder.HasIndex(j => j.TenantId);

                builder.HasOne(j => j.Tenant)
                    .WithMany()
                    .HasForeignKey(j => j.TenantId)
                    .OnDelete(DeleteBehavior.Restrict);

                builder.HasOne(j => j.User)
                    .WithMany()
                    .HasForeignKey(j => j.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                builder.HasOne(j => j.CardOrder)
                    .WithMany()
                    .HasForeignKey(j => j.CardOrderId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var clrType = entityType.ClrType;
                var isSoftDelete = typeof(ISoftDelete).IsAssignableFrom(clrType);
                var isTenantEntity = typeof(ITenantEntity).IsAssignableFrom(clrType);

                if (isSoftDelete || isTenantEntity)
                {
                    var parameter = Expression.Parameter(clrType, "e");
                    Expression? combinedBody = null;

                    // 1. Build Soft Delete Filter (IsDeleted == false)
                    if (isSoftDelete)
                    {
                        var isDeletedProp = Expression.Property(parameter, nameof(ISoftDelete.IsDeleted));
                        var falseConst = Expression.Constant(false);
                        combinedBody = Expression.Equal(isDeletedProp, falseConst);
                    }

                    // 2. Build Tenant Filter
                    if (isTenantEntity)
                    {
                        var tenantIdPropInfo = clrType.GetProperty("TenantId")
                            ?? throw new InvalidOperationException($"ITenantEntity {clrType.Name} does not have TenantId property.");

                        var tenantIdProp = Expression.Property(parameter, tenantIdPropInfo);
                        var dbContextExpr = Expression.Constant(this);
                        
                        var isSuperAdminExpr = Expression.Property(dbContextExpr, nameof(IsSuperAdmin));
                        var currentTenantIdExpr = Expression.Property(dbContextExpr, nameof(CurrentTenantId));

                        Expression tenantIdComparison;
                        if (tenantIdPropInfo.PropertyType == typeof(Guid?))
                        {
                            // Nullable Guid comparison (e.g. CardTemplate):
                            // _currentTenant.IsSuperAdmin || e.TenantId == null || e.TenantId == _currentTenant.TenantId
                            var nullConst = Expression.Constant(null, typeof(Guid?));
                            var isNullExpr = Expression.Equal(tenantIdProp, nullConst);
                            var currentTenantIdNullable = Expression.Convert(currentTenantIdExpr, typeof(Guid?));
                            var isMatchExpr = Expression.Equal(tenantIdProp, currentTenantIdNullable);
                            
                            var tenantMatchOrNull = Expression.OrElse(isNullExpr, isMatchExpr);
                            tenantIdComparison = Expression.OrElse(isSuperAdminExpr, tenantMatchOrNull);
                        }
                        else
                        {
                            // Non-nullable Guid comparison:
                            // _currentTenant.IsSuperAdmin || e.TenantId == _currentTenant.TenantId
                            var isMatchExpr = Expression.Equal(tenantIdProp, currentTenantIdExpr);
                            tenantIdComparison = Expression.OrElse(isSuperAdminExpr, isMatchExpr);
                        }

                        if (combinedBody == null)
                        {
                            combinedBody = tenantIdComparison;
                        }
                        else
                        {
                            combinedBody = Expression.AndAlso(combinedBody, tenantIdComparison);
                        }
                    }

                    if (combinedBody != null)
                    {
                        var lambda = Expression.Lambda(combinedBody, parameter);
                        modelBuilder.Entity(clrType).HasQueryFilter(lambda);
                    }
                }
            }
        }

        public override int SaveChanges()
        {
            ApplyTenantRules();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ApplyTenantRules();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void ApplyTenantRules()
        {
            var tenantId = _currentTenant.TenantId;

            foreach (var entry in ChangeTracker.Entries())
            {
                if (entry.Entity is ITenantEntity tenantEntity)
                {
                    if (entry.State == EntityState.Added)
                    {
                        if (tenantEntity.TenantId == Guid.Empty)
                        {
                            if (!tenantId.HasValue)
                            {
                                throw new InvalidOperationException("Cannot save tenant-scoped entity: current TenantId is not set.");
                            }
                            tenantEntity.TenantId = tenantId.Value;
                        }
                    }
                    else if (entry.State == EntityState.Modified)
                    {
                        var originalTenantId = entry.Property("TenantId").OriginalValue;
                        var currentTenantId = entry.Property("TenantId").CurrentValue;

                        if (!Equals(originalTenantId, currentTenantId))
                        {
                            throw new InvalidOperationException("Tenant ownership is immutable and cannot be changed after creation.");
                        }
                    }
                }

                if (entry.Entity is RefreshToken refreshToken)
                {
                    if (entry.State == EntityState.Added && refreshToken.TenantId == Guid.Empty)
                    {
                        if (!tenantId.HasValue)
                        {
                            throw new InvalidOperationException("Cannot save refresh token: current TenantId is not set.");
                        }
                        refreshToken.TenantId = tenantId.Value;
                    }
                }
            }
        }
    }
}
