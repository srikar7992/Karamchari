using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Performance.Domain.Career;

public sealed class CareerTrack : Entity<Guid>, ITenantOwned
{
    private readonly List<CareerLevel> _levels = [];

    private CareerTrack() { /* EF materialization */ }

    internal CareerTrack(
        Guid id,
        string tenantId,
        Guid frameworkId,
        string name,
        TrackType type,
        string? description) : base(id)
    {
        TenantId = tenantId;
        FrameworkId = frameworkId;
        Name = name;
        Type = type;
        Description = description;
    }

    public string TenantId { get; private set; } = string.Empty;
    public Guid FrameworkId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public TrackType Type { get; private set; }
    public string? Description { get; private set; }
    public IReadOnlyList<CareerLevel> Levels => _levels.AsReadOnly();

    internal CareerLevel AddLevel(string code, string displayName, int order, string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        if (_levels.Any(l => l.Order == order))
            throw new InvalidOperationException($"Level with order {order} already exists in this track.");

        var level = new CareerLevel(Guid.NewGuid(), Id, code.Trim().ToUpperInvariant(),
            displayName.Trim(), order, description?.Trim());
        _levels.Add(level);
        return level;
    }
}
