namespace Karamchari.Payroll.Domain.Compliance;

/// <summary>
/// Domain model representing a single row in the EPF ECR (Electronic Challan cum Return) file.
/// </summary>
public sealed record EcrRecord
{
    public string Uan { get; init; } = string.Empty;
    public string MemberName { get; init; } = string.Empty;
    public decimal GrossWages { get; init; }
    public decimal EpfWages { get; init; }
    public decimal EpsWages { get; init; }
    public decimal EdliWages { get; init; }
    public decimal EpfContribution { get; init; }
    public decimal EpsContribution { get; init; }
    public decimal EpfEpsDiff { get; init; }
    public int NcpDays { get; init; }
    public decimal RefundOfAdvances { get; init; }
}
