using Karamchari.Core.Multitenancy;

namespace Karamchari.TimeAttendance.Domain.Fraud;

/// <summary>
/// Represents a specific anomalous behavior detected during a punch event.
/// </summary>
public sealed class FraudSignal : ITenantOwned
{
    public string TenantId { get; private set; } = string.Empty;

    public Guid Id { get; private set; }

    public Guid EmployeeId { get; private set; }

    /// <summary>
    /// The type of signal (e.g., Velocity, Jump, Accuracy, DeviceSwitch).
    /// </summary>
    public string Type { get; private set; } = string.Empty;

    /// <summary>
    /// Risk score for this signal (0 to 100).
    /// </summary>
    public double Score { get; private set; }

    public string Reason { get; private set; } = string.Empty;

    public DateTime TimestampUtc { get; private set; }

    private FraudSignal() { }

    public static FraudSignal Create(string tenantId, Guid employeeId, string type, double score, string reason)
    {
        return new FraudSignal
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EmployeeId = employeeId,
            Type = type,
            Score = score,
            Reason = reason,
            TimestampUtc = DateTime.UtcNow
        };
    }
}
