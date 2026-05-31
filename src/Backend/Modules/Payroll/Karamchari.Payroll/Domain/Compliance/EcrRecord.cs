namespace Karamchari.Payroll.Domain.Compliance;

/// <summary>
/// Domain model representing a single row in the EPF ECR (Electronic Challan cum Return) file.
/// </summary>
public sealed record EcrRecord
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string Uan { get; init; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string MemberName { get; init; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal GrossWages { get; init; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal EpfWages { get; init; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal EpsWages { get; init; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal EdliWages { get; init; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal EpfContribution { get; init; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal EpsContribution { get; init; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal EpfEpsDiff { get; init; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public int NcpDays { get; init; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal RefundOfAdvances { get; init; }
}
