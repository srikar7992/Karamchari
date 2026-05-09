using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;
using Karamchari.Intelligence.Domain.Primitives;

namespace Karamchari.Intelligence.Domain.Signals;

/// <summary>
/// Value object representing the standardized pressure level (1-10) across organizational domains.
/// Prevents semantic drift between operational and strategic risk definitions.
/// </summary>
public sealed record WorkforcePressureIndex(int Level)
{
    public static WorkforcePressureIndex Create(int level)
    {
        if (level is < 1 or > 10)
            throw new ArgumentOutOfRangeException(nameof(level), "Pressure index must be between 1 and 10.");
        return new WorkforcePressureIndex(level);
    }
}

/// <summary>
/// Aggregate root representing the high-level health of an organizational unit.
/// Correlates signals from Attendance (OT), HR (Turnover), and Learning (Upskilling).
/// </summary>
public sealed class OrganizationalHealthSignal : AggregateRoot<Guid>, ITenantOwned
{
    public string TenantId { get; private set; } = string.Empty;
    public string OrgUnitId { get; private set; } = string.Empty; // Department or Team
    
    public decimal OverallHealthScore { get; private set; }
    public WorkforcePressureIndex BurnoutRisk { get; private set; } = null!;
    public WorkforcePressureIndex StaffingStress { get; private set; } = null!;
    
    public SignalConfidence Confidence { get; private set; } = null!;
    public ScoreExplanation Explanation { get; private set; } = null!;

    public DateTimeOffset EvaluatedAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    private OrganizationalHealthSignal() { }

    public static OrganizationalHealthSignal Evaluate(
        string tenantId,
        string orgUnitId,
        decimal score,
        WorkforcePressureIndex burnout,
        WorkforcePressureIndex staffing,
        SignalConfidence confidence,
        ScoreExplanation explanation)
    {
        return new OrganizationalHealthSignal
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OrgUnitId = orgUnitId,
            OverallHealthScore = score,
            BurnoutRisk = burnout,
            StaffingStress = staffing,
            Confidence = confidence,
            Explanation = explanation,
            EvaluatedAtUtc = DateTimeOffset.UtcNow
        };
    }
}
