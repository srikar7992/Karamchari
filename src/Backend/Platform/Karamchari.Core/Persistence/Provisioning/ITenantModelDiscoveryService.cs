namespace Karamchari.Core.Persistence.Provisioning;

/// <summary>
/// Service responsible for discovering all tenant-scoped relational artifacts directly from the EF Core model metadata.
/// </summary>
public interface ITenantModelDiscoveryService
{
    /// <summary>
    /// Discovers all tenant-scoped relational artifacts across all registered DbContexts.
    /// </summary>
    IReadOnlyCollection<TenantRelationalArtifact> Discover();
}
