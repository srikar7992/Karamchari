namespace Karamchari.TimeAttendance.Domain.Leaves;

using Karamchari.Core.Domain.Primitives;

/// <summary>
/// Immutable snapshot of a policy at a point in time. Never overwrite — append only.
/// </summary>
public sealed class LeavePolicyVersion : Entity<Guid>
{
    private LeavePolicyVersion()
    {
        ChangedBy = string.Empty;
        Rules = null!;
    }

    private LeavePolicyVersion(Guid id, Guid policyId, int version, LeavePolicyRules rules,
        DateOnly effectiveFrom, string changedBy, string? changeReason) : base(id)
    {
        PolicyId = policyId;
        Version = version;
        Rules = rules;
        EffectiveFrom = effectiveFrom;
        ChangedBy = changedBy;
        ChangeReason = changeReason;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid PolicyId { get; private set; }
    public int Version { get; private set; }
    public LeavePolicyRules Rules { get; private set; }
    public DateOnly EffectiveFrom { get; private set; }
    public DateOnly? EffectiveTo { get; private set; }
    public string ChangedBy { get; private set; }
    public string? ChangeReason { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static LeavePolicyVersion Create(Guid policyId, int version, LeavePolicyRules rules,
        DateOnly effectiveFrom, string changedBy, string? changeReason = null)
    {
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentException.ThrowIfNullOrWhiteSpace(changedBy);
        return new LeavePolicyVersion(Guid.NewGuid(), policyId, version, rules, effectiveFrom, changedBy, changeReason);
    }

    internal void Close(DateOnly closedOn) => EffectiveTo = closedOn;
}
