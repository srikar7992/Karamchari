using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Governance.Domain.Contracts;

public enum SchemaCompatibilityRule
{
    BackwardCompatible, // Consumers can read old and new
    ForwardCompatible,  // Old consumers can read new
    FullCompatible,     // Both
    None                // Breaking change
}

public enum SchemaStatus
{
    Draft,
    Active,
    Deprecated,
    Archived
}

/// <summary>
/// Aggregate root governing the contract payload schema for events and DTOs.
/// Enforces safe evolution and prevents downstream breakage.
/// </summary>
public sealed class SchemaDefinition : AggregateRoot<Guid>, ITenantOwned
{
    public string TenantId { get; private set; } = string.Empty; // "System" for global contracts
    public string ContractName { get; private set; } = string.Empty; // e.g., "EmployeeHiredEvent"
    public string Version { get; private set; } = string.Empty; // e.g., "v1.0"
    
    public string JsonSchema { get; private set; } = string.Empty;
    public SchemaCompatibilityRule CompatibilityRule { get; private set; }
    public SchemaStatus Status { get; private set; }

    public string OwnerModule { get; private set; } = string.Empty;
    
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? DeprecatedAtUtc { get; private set; }
    public DateTimeOffset? ArchivedAtUtc { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    private SchemaDefinition() { }

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

    public void Deprecate()
    {
        if (Status != SchemaStatus.Active)
            throw new InvalidOperationException($"Cannot deprecate schema in state {Status}");

        Status = SchemaStatus.Deprecated;
        DeprecatedAtUtc = DateTimeOffset.UtcNow;
        // Raise SchemaDeprecatedEvent to alert consumers
    }

    public void Archive()
    {
        if (Status != SchemaStatus.Deprecated)
            throw new InvalidOperationException("Only deprecated schemas can be archived.");

        Status = SchemaStatus.Archived;
        ArchivedAtUtc = DateTimeOffset.UtcNow;
    }
}
