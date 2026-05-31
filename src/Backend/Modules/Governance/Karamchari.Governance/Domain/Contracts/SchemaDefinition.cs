using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Governance.Domain.Contracts;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public enum SchemaCompatibilityRule
{
    /// <inheritdoc/>
    BackwardCompatible, // Consumers can read old and new
    /// <inheritdoc/>
    ForwardCompatible,  // Old consumers can read new
    /// <inheritdoc/>
    FullCompatible,     // Both
    /// <inheritdoc/>
    None                // Breaking change
}

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public enum SchemaStatus
{
    /// <inheritdoc/>
    Draft,
    /// <inheritdoc/>
    Active,
    /// <inheritdoc/>
    Deprecated,
    /// <inheritdoc/>
    Archived
}

/// <summary>
/// Aggregate root governing the contract payload schema for events and DTOs.
/// Enforces safe evolution and prevents downstream breakage.
/// </summary>
public sealed class SchemaDefinition : AggregateRoot<Guid>, ITenantOwned
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TenantId { get; private set; } = string.Empty; // "System" for global contracts
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string ContractName { get; private set; } = string.Empty; // e.g., "EmployeeHiredEvent"
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string Version { get; private set; } = string.Empty; // e.g., "v1.0"

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string JsonSchema { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public SchemaCompatibilityRule CompatibilityRule { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public SchemaStatus Status { get; private set; }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string OwnerModule { get; private set; } = string.Empty;

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; private set; }
    /// <inheritdoc/>
    public DateTimeOffset? DeprecatedAtUtc { get; private set; }
    /// <inheritdoc/>
    public DateTimeOffset? ArchivedAtUtc { get; private set; }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public byte[] RowVersion { get; private set; } = [];

    private SchemaDefinition() { }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static SchemaDefinition Register(
        string tenantId,
        string contractName,
        string version,
        string jsonSchema,
        SchemaCompatibilityRule compatibilityRule,
        string ownerModule)
    {
        return new SchemaDefinition
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ContractName = contractName,
            Version = version,
            JsonSchema = jsonSchema,
            CompatibilityRule = compatibilityRule,
            OwnerModule = ownerModule,
            Status = SchemaStatus.Active,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Deprecate()
    {
        if (Status != SchemaStatus.Active)
            throw new InvalidOperationException($"Cannot deprecate schema in state {Status}");

        Status = SchemaStatus.Deprecated;
        DeprecatedAtUtc = DateTimeOffset.UtcNow;
        // Raise SchemaDeprecatedEvent to alert consumers
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Archive()
    {
        if (Status != SchemaStatus.Deprecated)
            throw new InvalidOperationException("Only deprecated schemas can be archived.");

        Status = SchemaStatus.Archived;
        ArchivedAtUtc = DateTimeOffset.UtcNow;
    }
}
