using Karamchari.Core.Multitenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Text.Json;

namespace Karamchari.Core.Persistence.Interceptors;

/// <summary>
/// Interceptor to automatically record changes to entities for audit and compliance.
/// Records old and new values in a centralized audit table.
/// </summary>
public sealed class AuditInterceptor : SaveChangesInterceptor
{
    private readonly ITenantProvider _tenantProvider;

    public AuditInterceptor(ITenantProvider tenantProvider)
    {
        _tenantProvider = tenantProvider;
    }

    private bool _isSaving;

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null || _isSaving) return result;

        try
        {
            _isSaving = true;
            var auditEntries = CaptureChanges(eventData.Context);

            if (auditEntries.Count > 0)
            {
                foreach (var entry in auditEntries)
                {
                    eventData.Context.Set<AuditLog>().Add(entry.ToAuditLog());
                }
            }
        }
        finally
        {
            _isSaving = false;
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private List<AuditEntry> CaptureChanges(DbContext context)
    {
        // context.ChangeTracker.DetectChanges(); // Already called by EF before SavingChanges
        var entries = new List<AuditEntry>();

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.Entity is AuditLog || entry.State is EntityState.Detached or EntityState.Unchanged)
                continue;

            var auditEntry = new AuditEntry(entry)
            {
                TableName = entry.Metadata.GetTableName() ?? entry.Entity.GetType().Name,
                TenantId = (entry.Entity as ITenantOwned)?.TenantId ?? 
                           (_tenantProvider.TryGetTenant(out var t) ? t!.TenantId : "system"),
                UserId = "system", 
                Action = entry.State.ToString()
            };

            entries.Add(auditEntry);

            foreach (var property in entry.Properties)
            {
                string propertyName = property.Metadata.Name;
                if (property.Metadata.IsPrimaryKey())
                {
                    auditEntry.KeyValues[propertyName] = property.CurrentValue;
                    continue;
                }

                switch (entry.State)
                {
                    case EntityState.Added:
                        auditEntry.NewValues[propertyName] = property.CurrentValue;
                        break;

                    case EntityState.Deleted:
                        auditEntry.OldValues[propertyName] = property.OriginalValue;
                        break;

                    case EntityState.Modified:
                        if (property.IsModified)
                        {
                            auditEntry.OldValues[propertyName] = property.OriginalValue;
                            auditEntry.NewValues[propertyName] = property.CurrentValue;
                        }
                        break;
                }
            }
        }

        return entries;
    }
}

internal sealed class AuditEntry
{
    public AuditEntry(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        Entry = entry;
    }

    public Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry Entry { get; }
    public string TenantId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public Dictionary<string, object?> KeyValues { get; } = new();
    public Dictionary<string, object?> OldValues { get; } = new();
    public Dictionary<string, object?> NewValues { get; } = new();

    public AuditLog ToAuditLog()
    {
        return new AuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
            UserId = UserId,
            Action = Action,
            TableName = TableName,
            TimestampUtc = DateTimeOffset.UtcNow,
            PrimaryKey = JsonSerializer.Serialize(KeyValues),
            OldValues = OldValues.Count == 0 ? null : JsonSerializer.Serialize(OldValues),
            NewValues = NewValues.Count == 0 ? null : JsonSerializer.Serialize(NewValues)
        };
    }
}

public sealed class AuditLog
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public DateTimeOffset TimestampUtc { get; set; }
    public string PrimaryKey { get; set; } = string.Empty;
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
}
