using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Payroll.Domain.Simulation;

public enum SimulationType
{
    DryRunPayroll,
    RevisionImpact,
    TaxImpact,
    ArrearsEstimate,
    ComplianceImpact,
    BudgetForecast
}

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
/// Domain events are intentionally NOT raised — simulation must not trigger downstream workflows.
/// </summary>
public sealed class PayrollSimulation : AggregateRoot<Guid>
{
    private readonly List<SimulationEmployeeResult> _results = [];

    public Guid TenantId { get; private set; }
    public SimulationType Type { get; private set; }
    public SimulationStatus Status { get; private set; }
    public string Parameters { get; private set; } = string.Empty;  // JSON input params
    public string RequestedBy { get; private set; } = string.Empty;

    // Aggregate summary
    public decimal TotalProjectedGross { get; private set; }
    public decimal TotalProjectedNet { get; private set; }
    public decimal TotalProjectedTds { get; private set; }
    public decimal TotalProjectedDelta { get; private set; }  // vs current

    public string? ErrorMessage { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }

    // TTL: simulations auto-expire after 24h — never pollute live payroll data
    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public IReadOnlyCollection<SimulationEmployeeResult> Results => _results.AsReadOnly();

    private PayrollSimulation() { }

    public static PayrollSimulation Start(
        Guid tenantId,
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

    public void Fail(string error)
    {
        Status = SimulationStatus.Failed;
        ErrorMessage = error;
        CompletedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Discard()
    {
        Status = SimulationStatus.Discarded;
    }
}

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
    public decimal NetDelta => ProjectedNet - CurrentNet;
    public decimal GrossDelta => ProjectedGross - CurrentGross;
}
