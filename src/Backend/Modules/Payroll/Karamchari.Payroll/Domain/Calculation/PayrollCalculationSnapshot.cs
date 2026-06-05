using Karamchari.Core.Multitenancy;

namespace Karamchari.Payroll.Domain.Calculation;

/// <summary>
/// Immutable snapshot of all inputs used in a payroll run.
/// Enables deterministic reruns even if attendance or rates change after the run.
/// Never update this record.
/// </summary>
public sealed class PayrollCalculationSnapshot : ITenantOwned
{
    public string TenantId { get; private set; } = string.Empty;
    public Guid Id { get; private set; }
    public Guid PayrollRunId { get; private set; }
    public string SerializedData { get; private set; } = string.Empty;
    public DateTimeOffset SnapshotTakenAtUtc { get; private set; }

    private PayrollCalculationSnapshot() { }

    public static PayrollCalculationSnapshot Take(string tenantId, Guid payrollRunId, string serializedData)
    {
        if (string.IsNullOrWhiteSpace(serializedData))
            throw new ArgumentException("Serialized data must not be empty.");

        return new PayrollCalculationSnapshot
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PayrollRunId = payrollRunId,
            SerializedData = serializedData,
            SnapshotTakenAtUtc = DateTimeOffset.UtcNow
        };
    }
}
