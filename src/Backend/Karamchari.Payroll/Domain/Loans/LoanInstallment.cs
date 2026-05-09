namespace Karamchari.Payroll.Domain.Loans;

public sealed class LoanInstallment
{
    public Guid Id { get; private set; }
    public int InstallmentNumber { get; private set; }
    public string PeriodName { get; private set; } = string.Empty;
    public int Year { get; private set; }
    public int Month { get; private set; }
    public decimal PrincipalAmount { get; private set; }
    public decimal InterestAmount { get; private set; }
    public decimal TotalAmount => PrincipalAmount + InterestAmount;
    public decimal OutstandingAfter { get; private set; }
    public InstallmentStatus Status { get; private set; }
    public DateTimeOffset? DeductedAtUtc { get; private set; }
    public string? SkipReason { get; private set; }

    private LoanInstallment() { }

    public static LoanInstallment Create(
        int number,
        string periodName,
        int year,
        int month,
        decimal principal,
        decimal interest,
        decimal outstandingAfter)
    {
        return new LoanInstallment
        {
            Id = Guid.NewGuid(),
            InstallmentNumber = number,
            PeriodName = periodName,
            Year = year,
            Month = month,
            PrincipalAmount = principal,
            InterestAmount = interest,
            OutstandingAfter = outstandingAfter,
            Status = InstallmentStatus.Pending
        };
    }

    public void MarkDeducted()
    {
        Status = InstallmentStatus.Deducted;
        DeductedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Skip(string reason)
    {
        Status = InstallmentStatus.Skipped;
        SkipReason = reason;
    }

    public void CarryForward()
    {
        Status = InstallmentStatus.CarriedForward;
    }

    public void Waive()
    {
        Status = InstallmentStatus.Waived;
    }
}
