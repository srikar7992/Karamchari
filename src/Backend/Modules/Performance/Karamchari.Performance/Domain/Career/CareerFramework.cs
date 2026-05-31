using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Performance.Domain.Career;

/// <summary>
/// Defines the entire career ladder for a tenant: tracks, levels, and competency requirements.
/// One active framework per tenant (enforced at app layer).
/// </summary>
public sealed class CareerFramework : AggregateRoot<Guid>, ITenantOwned
{
    private readonly List<CareerTrack> _tracks = [];

    private CareerFramework() { /* EF materialization */ }

    private CareerFramework(Guid id, string tenantId, string name, string? description) : base(id)
    {
        TenantId = tenantId;
        Name = name;
        Description = description;
        IsActive = true;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TenantId { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public bool IsActive { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public byte[] RowVersion { get; private set; } = [];
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public IReadOnlyList<CareerTrack> Tracks => _tracks.AsReadOnly();

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static CareerFramework Create(string tenantId, string name, string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new CareerFramework(Guid.NewGuid(), tenantId, name.Trim(), description?.Trim());
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public CareerTrack AddTrack(string name, TrackType type, string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (_tracks.Any(t => t.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase) && t.Type == type))
            throw new InvalidOperationException($"Track '{name}' with type {type} already exists.");

        var track = new CareerTrack(Guid.NewGuid(), TenantId, Id, name.Trim(), type, description?.Trim());
        _tracks.Add(track);
        return track;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Deactivate() => IsActive = false;
}
