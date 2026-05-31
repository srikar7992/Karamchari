namespace Karamchari.Core.Persistence.Provisioning;

/// <summary>
/// Describes a relational artifact (table) discovered from the EF Core model that requires tenant provisioning.
/// </summary>
public sealed record TenantRelationalArtifact
{
    public required string DbContextName { get; init; }
    public required string EntityName { get; init; }
    public required string TableName { get; init; }
    public required string Schema { get; init; }
    public bool IsOwned { get; init; }
    public bool IsOwnedCollection { get; init; }
    public bool IsJoinTable { get; init; }
    public bool IsTenantScoped { get; init; }
}
