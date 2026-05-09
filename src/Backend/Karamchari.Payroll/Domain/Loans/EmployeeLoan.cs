using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Domain.Workflows;
using Karamchari.Payroll.Domain.Loans.Events;

namespace Karamchari.Payroll.Domain.Loans;

/// <summary>
/// Aggregate for employee loans and salary advances.
/// Maintains the full amortization schedule. Installments are computed externally
/// (LoanAmortizationEngine) and injected via SetSchedule().
/// </summary>
public sealed class EmployeeLoan : AggregateRoot<Guid>
{
    private readonly List<LoanInstallment> _installments = [];
    private readonly List<ApprovalStep> _approvalChain = [];

    public string TenantId { get; private set; } = string.Empty;
    public Guid EmployeeId { get; private set; }
    public string EmployeeName { get; private set; } = string.Empty;
    public LoanType Type { get; private set; }
    public LoanInterestType InterestType { get; private set; }
    public LoanStatus Status { get; private set; }

    public decimal PrincipalAmount { get; private set; }
    public decimal InterestRatePercent { get; private set; }
    public int TenureMonths { get; private set; }
    public decimal MonthlyEmi { get; private set; }
    public decimal OutstandingBalance { get; private set; }

    public DateOnly DisbursedOn { get; private set; }
    public string? ApprovedBy { get; private set; }
    public DateTimeOffset? ApprovedAtUtc { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public IReadOnlyCollection<LoanInstallment> Installments => _installments.AsReadOnly();
    public IReadOnlyCollection<ApprovalStep> ApprovalChain => _approvalChain.AsReadOnly();

    private EmployeeLoan() { }

    public static EmployeeLoan Request(
        string tenantId,
        Guid employeeId,
        string employeeName,
        LoanType type,
        LoanInterestType interestType,
        decimal principalAmount,
        decimal interestRatePercent,
        int tenureMonths,
        DateOnly disbursedOn)
    {
        var loan = new EmployeeLoan
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EmployeeId = employeeId,
            EmployeeName = employeeName,
            Type = type,
            InterestType = interestType,
            PrincipalAmount = principalAmount,
            InterestRatePercent = interestRatePercent,
            TenureMonths = tenureMonths,
            OutstandingBalance = principalAmount,
            DisbursedOn = disbursedOn,
            Status = LoanStatus.PendingApproval,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        // Standard loan chain: Manager -> HR -> Finance
        loan._approvalChain.Add(ApprovalStep.Create(1, "Manager"));
        loan._approvalChain.Add(ApprovalStep.Create(2, "HR"));
        loan._approvalChain.Add(ApprovalStep.Create(3, "Finance"));

        loan.RaiseDomainEvent(new LoanCreatedEvent(
            loan.Id, tenantId, employeeId, type, principalAmount));

        return loan;
    }

    public void Approve(string approvedBy, string? note = null)
    {
        if (Status != LoanStatus.PendingApproval)
            throw new InvalidOperationException($"Cannot approve loan in status {Status}.");

        var currentStep = _approvalChain
            .OrderBy(s => s.Sequence)
            .FirstOrDefault(s => s.Status == ApprovalStatus.Pending)
            ?? throw new InvalidOperationException("No pending approval steps found.");

        currentStep.Approve(approvedBy, note);

        // If all steps approved, activate loan
        if (_approvalChain.All(s => s.Status == ApprovalStatus.Approved))
        {
            Status = LoanStatus.Active;
            ApprovedBy = approvedBy; // Last approver
            ApprovedAtUtc = DateTimeOffset.UtcNow;
            RaiseDomainEvent(new LoanClosedEvent(Id, TenantId, EmployeeId, LoanStatus.Active)); // Using existing event for activation notify
        }

        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Reject(string rejectedBy, string reason)
    {
        if (Status != LoanStatus.PendingApproval)
            throw new InvalidOperationException($"Cannot reject loan in status {Status}.");

        var currentStep = _approvalChain
            .OrderBy(s => s.Sequence)
            .FirstOrDefault(s => s.Status == ApprovalStatus.Pending)
            ?? throw new InvalidOperationException("No pending approval steps found.");

        currentStep.Reject(rejectedBy, reason);

        Status = LoanStatus.Rejected;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void SetSchedule(IEnumerable<LoanInstallment> installments, decimal monthlyEmi)
    {
        if (_installments.Count > 0)
            throw new InvalidOperationException("Schedule already set. Use Restructure() to modify.");

        _installments.AddRange(installments);
        MonthlyEmi = monthlyEmi;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void DeductInstallment(string periodName, int year, int month)
    {
        if (Status != LoanStatus.Active)
            throw new InvalidOperationException($"Cannot deduct from loan in status {Status}.");

        var installment = _installments.FirstOrDefault(
            i => i.Year == year && i.Month == month && i.Status == InstallmentStatus.Pending)
            ?? throw new InvalidOperationException($"No pending installment for period {year}/{month}.");

        installment.MarkDeducted();
        OutstandingBalance = Math.Max(0, OutstandingBalance - installment.PrincipalAmount);
        UpdatedAtUtc = DateTimeOffset.UtcNow;

        if (OutstandingBalance == 0)
        {
            Status = LoanStatus.ClosedByEmployee;
            RaiseDomainEvent(new LoanClosedEvent(Id, TenantId, EmployeeId, LoanStatus.ClosedByEmployee));
        }
    }

    public void SkipInstallment(string periodName, int year, int month, string reason)
    {
        var installment = _installments.FirstOrDefault(
            i => i.Year == year && i.Month == month && i.Status == InstallmentStatus.Pending)
            ?? throw new InvalidOperationException($"No pending installment for {year}/{month}.");

        installment.Skip(reason);
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void PreClose(decimal paymentAmount)
    {
        if (Status != LoanStatus.Active)
            throw new InvalidOperationException($"Cannot pre-close loan in status {Status}.");

        if (paymentAmount < OutstandingBalance)
            throw new InvalidOperationException($"Pre-close payment {paymentAmount} is less than outstanding {OutstandingBalance}.");

        // Mark remaining pending installments as waived
        foreach (var installment in _installments.Where(i => i.Status == InstallmentStatus.Pending))
            installment.Waive();

        OutstandingBalance = 0;
        Status = LoanStatus.PreClosed;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new LoanClosedEvent(Id, TenantId, EmployeeId, LoanStatus.PreClosed));
    }

    public void CloseOnExit()
    {
        // Remaining balance collected via FnF loan recovery line item
        foreach (var installment in _installments.Where(i => i.Status == InstallmentStatus.Pending))
            installment.Waive();

        OutstandingBalance = 0;
        Status = LoanStatus.ClosedByExit;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new LoanClosedEvent(Id, TenantId, EmployeeId, LoanStatus.ClosedByExit));
    }
}
