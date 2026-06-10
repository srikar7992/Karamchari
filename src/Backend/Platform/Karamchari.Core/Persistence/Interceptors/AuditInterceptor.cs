// -----------------------------------------------------------------------
// <copyright file="AuditInterceptor.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.Json;
using Karamchari.Core.Multitenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Karamchari.Core.Persistence.Interceptors;

/// <summary>
/// Interceptor to automatically record changes to entities for audit and compliance.
/// Records old and new values in a centralized audit table.
/// </summary>
public sealed class AuditInterceptor : SaveChangesInterceptor
{
    private readonly ITenantProvider _tenantProvider;

    /// <summary>
    /// Initializes a new instance of the AuditInterceptor.
    /// </summary>
    /// <param name="tenantProvider">The tenant provider.</param>
    public AuditInterceptor(ITenantProvider tenantProvider)
    {
        _tenantProvider = tenantProvider;
    }

    private bool _isSaving;

    /// <summary>
    /// Intercepts the SaveChangesAsync call to write audit entries.
    /// </summary>
    /// <param name="eventData">The event data.</param>
    /// <param name="result">The interception result.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The intercepted result.</returns>
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventData);
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
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TenantId { get; set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string UserId { get; set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TableName { get; set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string Action { get; set; } = string.Empty;
    public Dictionary<string, object?> KeyValues { get; } = new();
    public Dictionary<string, object?> OldValues { get; } = new();
    public Dictionary<string, object?> NewValues { get; } = new();

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
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

/// <summary>
/// Represents a persisted audit log record.
/// </summary>
public sealed class AuditLog
{
    /// <summary>Unique identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Tenant identifier.</summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>User who made the change.</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>The action performed (Added, Modified, Deleted).</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>Name of the affected table.</summary>
    public string TableName { get; set; } = string.Empty;

    /// <summary>UTC timestamp of the change.</summary>
    public DateTimeOffset TimestampUtc { get; set; }

    /// <summary>Primary key of the affected record, serialized as JSON.</summary>
    public string PrimaryKey { get; set; } = string.Empty;

    /// <summary>Original values, serialized as JSON.</summary>
    public string? OldValues { get; set; }

    /// <summary>New values, serialized as JSON.</summary>
    public string? NewValues { get; set; }
}
