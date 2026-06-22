using Karamchari.Core.Multitenancy;
using Karamchari.Core.Persistence;
using Karamchari.Extensibility.Domain;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Extensibility.Persistence;

public sealed class ExtensibilityDbContext : KaramchariDbContext
{
    public ExtensibilityDbContext(DbContextOptions<ExtensibilityDbContext> options, ITenantProvider tenantProvider)
        : base(options, tenantProvider) { }

    public DbSet<CustomEntityDefinition> EntityDefinitions => Set<CustomEntityDefinition>();
    public DbSet<CustomEntityRecord> EntityRecords => Set<CustomEntityRecord>();

    protected override void OnDomainModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnDomainModelCreating(modelBuilder);

        modelBuilder.Entity<CustomEntityDefinition>(b =>
        {
            b.ToTable("CustomEntityDefinitions");
            b.HasKey(x => x.Id);
            b.Property(x => x.TenantId).IsRequired().HasMaxLength(64);
            b.Property(x => x.EntityName).IsRequired().HasMaxLength(128);
            b.Property(x => x.Description).HasMaxLength(500);
            b.HasIndex(x => new { x.TenantId, x.EntityName })
                .IsUnique().HasDatabaseName("UIX_CustomEntityDefs_Tenant_Name");
            b.OwnsMany(x => x.Fields, f =>
            {
                f.ToTable("CustomFieldDefinitions");
                f.HasKey(x => x.Id);
                f.Property(x => x.DefinitionId).IsRequired();
                f.Property(x => x.FieldName).IsRequired().HasMaxLength(128);
                f.Property(x => x.FieldType).HasConversion<string>().HasMaxLength(20);
                f.Property(x => x.DefaultValue).HasMaxLength(500);
                f.HasIndex(x => new { x.DefinitionId, x.FieldName })
                    .IsUnique().HasDatabaseName("UIX_CustomFieldDefs_Def_Name");
            });
        });

        modelBuilder.Entity<CustomEntityRecord>(b =>
        {
            b.ToTable("CustomEntityRecords");
            b.HasKey(x => x.Id);
            b.Property(x => x.TenantId).IsRequired().HasMaxLength(64);
            b.Property(x => x.Payload).HasColumnType("nvarchar(max)").IsRequired();
            b.HasIndex(x => new { x.TenantId, x.DefinitionId }).HasDatabaseName("IX_CustomEntityRecords_Tenant_Def");
            b.HasIndex(x => new { x.TenantId, x.OwnerEntityId }).HasDatabaseName("IX_CustomEntityRecords_Tenant_Owner");
        });
    }
}
