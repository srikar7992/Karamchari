using Karamchari.Core.Multitenancy;

namespace Karamchari.TimeAttendance.Domain.Fraud;

/// <summary>
/// A definitive fraud assessment for a specific punch event.
/// </summary>
public sealed class FraudFlag : ITenantOwned
{
    public string TenantId { get; private set; } = string.Empty;

    public Guid Id { get; private set; }

    public Guid RawPunchId { get; private set; }

    public string Severity { get; private set; } = string.Empty; // Safe, Suspicious, Fraud

    public double TotalRiskScore { get; private set; }

    public string Explanation { get; private set; } = string.Empty;

    public DateTime CreatedAtUtc { get; private set; }

    private FraudFlag() { }

    public static FraudFlag Create(string tenantId, Guid rawPunchId, string severity, double score, string explanation)
    {
        return new FraudFlag
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            RawPunchId = rawPunchId,
            Severity = severity,
            TotalRiskScore = score,
            Explanation = explanation,
            CreatedAtUtc = DateTime.UtcNow
        };
    }
}
