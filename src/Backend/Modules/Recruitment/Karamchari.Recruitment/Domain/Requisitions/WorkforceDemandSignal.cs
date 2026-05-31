using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Recruitment.Domain.Requisitions;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public enum WorkforceDemandLevel
{
    /// <inheritdoc/>
    Normal,
    /// <inheritdoc/>
    High,
    /// <inheritdoc/>
    Critical
}

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public enum WorkforceDemandSource
{
    /// <inheritdoc/>
    ManagerRequest,
    /// <inheritdoc/>
    OvertimeAlert,
    /// <inheritdoc/>
    AttritionForecast,
    /// <inheritdoc/>
    ExpansionPlan
}

/// <summary>
/// Represents a validated operational signal justifying the hiring demand.
/// Requisitions linked to critical demand signals can bypass certain financial freezes.
/// </summary>
public sealed class WorkforceDemandSignal : Entity<Guid>
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid RequisitionId { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public WorkforceDemandSource Source { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public WorkforceDemandLevel Level { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string Justification { get; private set; } = string.Empty;
    /// <inheritdoc/>
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
