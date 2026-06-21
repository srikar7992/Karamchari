using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Governance.Domain.Controls;

public sealed class SodMatrix : AggregateRoot<Guid>, ITenantOwned
{
    private readonly List<SodRule> _rules = [];
    private SodMatrix() { }

    public string TenantId { get; private set; } = string.Empty;
    public IReadOnlyList<SodRule> Rules => _rules.AsReadOnly();

    public static SodMatrix Create(string tenantId)
        => new() { Id = Guid.NewGuid(), TenantId = tenantId };

    public void AddRule(string role1, string role2, string reason)
        => _rules.Add(new SodRule(role1, role2, reason));

    public bool Conflicts(string roleBeingAssigned, IEnumerable<string> existingRoles)
        => _rules.Any(r =>
            (r.Role1 == roleBeingAssigned && existingRoles.Contains(r.Role2)) ||
            (r.Role2 == roleBeingAssigned && existingRoles.Contains(r.Role1)));
}
