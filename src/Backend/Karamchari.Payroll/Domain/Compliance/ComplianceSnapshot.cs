using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Payroll.Domain.Compliance;

public enum ComplianceType
{
    EpfEcr,
    EsicMonthly,
    TdsForm24Q
}

/// <summary>
/// Persisted snapshot of a generated compliance file for audit and repeatability.
/// </summary>
public sealed class ComplianceSnapshot : Entity<Guid>, ITenantOwned
{
    public string TenantId { get; private set; } = string.Empty;
    public Guid PayrollRunId { get; private set; }
    public ComplianceType Type { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public byte[] Content { get; private set; } = Array.Empty<byte>();
    public DateTime GeneratedAtUtc { get; private set; }
    public string GeneratedBy { get; private set; } = string.Empty;

    private ComplianceSnapshot() { }

    public static ComplianceSnapshot Create(
        string tenantId,
        Guid payrollRunId,
        ComplianceType type,
        string fileName,
        byte[] content,
        string generatedBy)
    {
        return new ComplianceSnapshot
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PayrollRunId = payrollRunId,
            Type = type,
            FileName = fileName,
            Content = content,
            GeneratedAtUtc = DateTime.UtcNow,
            GeneratedBy = generatedBy
        };
    }
}
