using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Payroll.Domain.Simulation;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public enum SimulationType
{
    DryRunPayroll,
    RevisionImpact,
    TaxImpact,
    ArrearsEstimate,
    ComplianceImpact,
    BudgetForecast
}

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public enum SimulationStatus
{
    Running,
    Completed,
    Failed,
    Discarded
}

/// <summary>
/// Ephemeral simulation aggregate. NEVER mutates real payroll state.
/// Simulation results are isolated projections stored here only.
/// Domain events are intentionally NOT raised â€” simulation must not trigger downstream workflows.
/// </summary>
public sealed class PayrollSimulation : AggregateRoot<Guid>
{
    private readonly List<SimulationEmployeeResult> _results = [];

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TenantId { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public SimulationType Type { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public SimulationStatus Status { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string Parameters { get; private set; } = string.Empty;  // JSON input params
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string RequestedBy { get; private set; } = string.Empty;

    // Aggregate summary
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal TotalProjectedGross { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal TotalProjectedNet { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal TotalProjectedTds { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal TotalProjectedDelta { get; private set; }  // vs current

    public string? ErrorMessage { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }

    // TTL: simulations auto-expire after 24h â€” never pollute live payroll data
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateTimeOffset ExpiresAtUtc { get; private set; }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public IReadOnlyCollection<SimulationEmployeeResult> Results => _results.AsReadOnly();

    private PayrollSimulation() { }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static PayrollSimulation Start(
        string tenantId,
        SimulationType type,
        string parametersJson,
        string requestedBy)
    {
        return new PayrollSimulation
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Type = type,
            Parameters = parametersJson,
            RequestedBy = requestedBy,
            Status = SimulationStatus.Running,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddHours(24)
        };
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Complete(IEnumerable<SimulationEmployeeResult> results)
    {
        if (Status != SimulationStatus.Running)
            throw new InvalidOperationException($"Cannot complete simulation in status {Status}.");

        _results.AddRange(results);
        TotalProjectedGross = _results.Sum(r => r.ProjectedGross);
        TotalProjectedNet = _results.Sum(r => r.ProjectedNet);
        TotalProjectedTds = _results.Sum(r => r.ProjectedTds);
        TotalProjectedDelta = _results.Sum(r => r.NetDelta);
        Status = SimulationStatus.Completed;
        CompletedAtUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Fail(string error)
    {
        Status = SimulationStatus.Failed;
        ErrorMessage = error;
        CompletedAtUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Discard()
    {
        Status = SimulationStatus.Discarded;
    }
}

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed record SimulationEmployeeResult(
    Guid EmployeeId,
    string EmployeeName,
    decimal CurrentGross,
    decimal ProjectedGross,
    decimal CurrentNet,
    decimal ProjectedNet,
    decimal CurrentTds,
    decimal ProjectedTds,
    IReadOnlyDictionary<string, decimal> ComponentBreakdown)
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal NetDelta => ProjectedNet - CurrentNet;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal GrossDelta => ProjectedGross - CurrentGross;
}
