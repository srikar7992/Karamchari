using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Capability.Domain.Entitlements;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public enum CapabilityTier
{
    /// <inheritdoc/>
    Foundation,
    /// <inheritdoc/>
    Standard,
    /// <inheritdoc/>
    Premium,
    /// <inheritdoc/>
    Enterprise
}

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed class CapabilityDefinition : AggregateRoot<Guid>
{
    private readonly List<string> _dependencies = [];
    private readonly List<string> _permissions = [];
    private readonly List<string> _routes = [];

    private CapabilityDefinition() { /* EF */ }

    private CapabilityDefinition(
        Guid id,
        string capabilityId,
        string displayName,
        CapabilityTier tier,
        bool requiresLicense) : base(id)
    {
        CapabilityId = capabilityId;
        DisplayName = displayName;
        Tier = tier;
        RequiresLicense = requiresLicense;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string CapabilityId { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string DisplayName { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public CapabilityTier Tier { get; private set; }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public IReadOnlyCollection<string> Dependencies => _dependencies.AsReadOnly();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public IReadOnlyCollection<string> Permissions => _permissions.AsReadOnly();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public IReadOnlyCollection<string> Routes => _routes.AsReadOnly();

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public bool RequiresLicense { get; private set; }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static CapabilityDefinition Create(string capabilityId, string displayName, CapabilityTier tier, bool requiresLicense)
    {
        return new CapabilityDefinition(Guid.NewGuid(), capabilityId, displayName, tier, requiresLicense);
    }
}
