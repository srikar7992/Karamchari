using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Recruitment.Domain.Requisitions;

public enum WorkforceDemandLevel
{
    Normal,
    High,
    Critical
}

public enum WorkforceDemandSource
{
    ManagerRequest,
    OvertimeAlert,
    AttritionForecast,
    ExpansionPlan
}

/// <summary>
/// Represents a validated operational signal justifying the hiring demand.
/// Requisitions linked to critical demand signals can bypass certain financial freezes.
/// </summary>
public sealed class WorkforceDemandSignal : Entity<Guid>
{
    public Guid RequisitionId { get; private set; }
    public WorkforceDemandSource Source { get; private set; }
    public WorkforceDemandLevel Level { get; private set; }
    public string Justification { get; private set; } = string.Empty;
    public string? SourceReferenceId { get; private set; } // e.g. An Overtime Anomaly ID

    private WorkforceDemandSignal() { }

    internal static WorkforceDemandSignal Create(
        Guid requisitionId,
        WorkforceDemandSource source,
        WorkforceDemandLevel level,
        string justification,
        string? sourceReferenceId = null)
    {
        return new WorkforceDemandSignal
        {
            Id = Guid.NewGuid(),
            RequisitionId = requisitionId,
            Source = source,
            Level = level,
            Justification = justification,
            SourceReferenceId = sourceReferenceId
        };
    }
}
