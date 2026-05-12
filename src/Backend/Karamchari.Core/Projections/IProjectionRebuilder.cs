namespace Karamchari.Core.Projections;

/// <summary>
/// Defines the contract for a service that can rebuild a specific projection.
/// Implements Priority 1 Step 4.
/// </summary>
public interface IProjectionRebuilder
{
    /// <summary>Gets the metadata for the projection this rebuilder handles.</summary>
    ProjectionMetadata Metadata { get; }

    /// <summary>
    /// Fully or partially rebuilds the projection for a specific tenant.
    /// </summary>
    /// <param name="tenantId">The tenant to rebuild for.</param>
    /// <param name="startTime">Optional start time for incremental rebuild.</param>
    /// <param name="endTime">Optional end time for windowed rebuild.</param>
    /// <param name="ct">The cancellation token.</param>
    Task RebuildAsync(
        string tenantId,
        DateTimeOffset? startTime = null,
        DateTimeOffset? endTime = null,
        CancellationToken ct = default);
}

/// <summary>
/// Registry for all projection rebuilders in the platform.
/// Enables centralized rebuild orchestration.
/// </summary>
public interface IProjectionRegistry
{
    IReadOnlyCollection<ProjectionMetadata> GetAllMetadata();

    IProjectionRebuilder? GetRebuilder(string projectionName);
}
