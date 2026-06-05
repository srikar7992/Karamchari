namespace Karamchari.TimeAttendance.Domain.Leaves;

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

/// <summary>
/// Payroll integration boundary. Summarizes leave impact per employee per pay period.
/// Payroll module reads this — never reads LeaveBalance or LeaveRequest directly.
/// Contains: LOP days, LWP days, encashment days, comp-off payout, paid leave days.
/// </summary>
public sealed class PayrollLeaveAdvisory : AggregateRoot<Guid>, ITenantOwned
{
    private readonly List<PayrollLeaveLine> _lines = [];

    private PayrollLeaveAdvisory(Guid id, Guid employeeId, string payPeriod,
        DateOnly periodStart, DateOnly periodEnd) : base(id)
    {
        EmployeeId = employeeId;
        PayPeriod = payPeriod;
        PeriodStart = periodStart;
        PeriodEnd = periodEnd;
        Status = PayrollAdvisoryStatus.Draft;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    private PayrollLeaveAdvisory()
    {
        TenantId = string.Empty;
        PayPeriod = string.Empty;
    }

    public string TenantId { get; private set; } = string.Empty;
    public Guid EmployeeId { get; private set; }

    /// <summary>Human-readable period key, e.g. "2026-05" or "2026-Q2".</summary>
    public string PayPeriod { get; private set; }

    public DateOnly PeriodStart { get; private set; }
    public DateOnly PeriodEnd { get; private set; }
    public PayrollAdvisoryStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? FinalizedAtUtc { get; private set; }

    public IReadOnlyCollection<PayrollLeaveLine> Lines => _lines.AsReadOnly();

    public decimal TotalLopDays => _lines.Where(l => l.LineType == PayrollLeaveLineType.LOP).Sum(l => l.Days);
    public decimal TotalLwpDays => _lines.Where(l => l.LineType == PayrollLeaveLineType.LWP).Sum(l => l.Days);
    public decimal TotalEncashedDays => _lines.Where(l => l.LineType == PayrollLeaveLineType.Encashment).Sum(l => l.Days);

    public static PayrollLeaveAdvisory Create(Guid employeeId, string payPeriod,
        DateOnly periodStart, DateOnly periodEnd)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(payPeriod);
        if (periodEnd < periodStart)
            throw new ArgumentException("Period end must be after period start.");
        return new PayrollLeaveAdvisory(Guid.NewGuid(), employeeId, payPeriod, periodStart, periodEnd);
    }

    public void AddLine(PayrollLeaveLineType lineType, decimal days,
        Guid? leaveRequestId = null, string? notes = null)
    {
        if (Status != PayrollAdvisoryStatus.Draft)
            throw new InvalidOperationException("Cannot modify a finalized advisory.");
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(days);

        _lines.Add(PayrollLeaveLine.Create(Id, lineType, days, leaveRequestId, notes));
    }

    public void FinalizeAdvisory()
    {
        if (Status != PayrollAdvisoryStatus.Draft)
            throw new InvalidOperationException("Advisory already finalized.");
        Status = PayrollAdvisoryStatus.Finalized;
        FinalizedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Reopen()
    {
        if (Status != PayrollAdvisoryStatus.Finalized)
            throw new InvalidOperationException("Can only reopen finalized advisories.");
        Status = PayrollAdvisoryStatus.Draft;
        FinalizedAtUtc = null;
    }
}

public sealed class PayrollLeaveLine : Entity<Guid>
{
    private PayrollLeaveLine() { }

    private PayrollLeaveLine(Guid id, Guid advisoryId, PayrollLeaveLineType lineType,
        decimal days, Guid? leaveRequestId, string? notes) : base(id)
    {
        AdvisoryId = advisoryId;
        LineType = lineType;
        Days = days;
        LeaveRequestId = leaveRequestId;
        Notes = notes;
    }

    public Guid AdvisoryId { get; private set; }
    public PayrollLeaveLineType LineType { get; private set; }
    public decimal Days { get; private set; }
    public Guid? LeaveRequestId { get; private set; }
    public string? Notes { get; private set; }

    internal static PayrollLeaveLine Create(Guid advisoryId, PayrollLeaveLineType lineType,
        decimal days, Guid? leaveRequestId, string? notes)
        => new(Guid.NewGuid(), advisoryId, lineType, days, leaveRequestId, notes);
}

public enum PayrollLeaveLineType
{
    LOP = 1,
    LWP = 2,
    Encashment = 3,
    CompOffPayout = 4,
    PaidLeave = 5
}

public enum PayrollAdvisoryStatus
{
    Draft = 1,
    Finalized = 2,
    ExportedToPayroll = 3
}
