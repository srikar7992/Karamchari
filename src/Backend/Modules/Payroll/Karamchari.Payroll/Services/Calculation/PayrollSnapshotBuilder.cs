using System.Text.Json;
using Karamchari.Payroll.Domain.Calculation;

namespace Karamchari.Payroll.Services.Calculation;

/// <summary>
/// Builds an immutable PayrollCalculationSnapshot before a run starts.
/// Snapshot freezes: attendance, leave, holidays, rates, OT policy, shift premiums.
/// All subsequent calculation reads from snapshot, never live records.
/// </summary>
public interface IPayrollSnapshotBuilder
{
    Task<PayrollCalculationSnapshot> BuildAsync(
        string tenantId,
        Guid payrollRunId,
        IReadOnlyList<EmployeePayrollInput> inputs,
        CancellationToken cancellationToken = default);
}

public sealed class PayrollSnapshotBuilder : IPayrollSnapshotBuilder
{
    public Task<PayrollCalculationSnapshot> BuildAsync(
        string tenantId,
        Guid payrollRunId,
        IReadOnlyList<EmployeePayrollInput> inputs,
        CancellationToken cancellationToken = default)
    {
        var serialized = JsonSerializer.Serialize(inputs);
        var snapshot = PayrollCalculationSnapshot.Take(tenantId, payrollRunId, serialized);
        return Task.FromResult(snapshot);
    }
}
