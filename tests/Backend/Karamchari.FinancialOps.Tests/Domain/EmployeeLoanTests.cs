using FluentAssertions;
using Karamchari.FinancialOps.Contracts.Primitives;
using Karamchari.FinancialOps.Domain.Ledger;
using Xunit;

namespace Karamchari.FinancialOps.Tests.Domain;

public sealed class EmployeeLoanTests
{
    // ─── Helpers ────────────────────────────────────────────────────────────────

    private static EmployeeLoan CreateLoan(
        string tenantId = "tenant-1",
        decimal principal = 100_000m,
        decimal interestRate = 0.08m,
        int tenureMonths = 12)
    {
        return EmployeeLoan.Create(
            tenantId: tenantId,
            employeeId: new EmployeeId(Guid.NewGuid()),
            principalAmount: principal,
            interestRate: interestRate,
            tenureMonths: tenureMonths);
    }

    private static FinancialCorrelationChain NewChain() =>
        FinancialCorrelationChain.CreateNew();

    // ─── Create ──────────────────────────────────────────────────────────────────

    [Fact]
    public void EmployeeLoan_Create_RemainingPrincipalEqualsPrincipalAmount()
    {
        // Arrange & Act
        var loan = CreateLoan(principal: 50_000m);

        // Assert
        loan.RemainingPrincipal.Should().Be(50_000m);
        loan.PrincipalAmount.Should().Be(50_000m);
    }

    [Fact]
    public void EmployeeLoan_Create_StatusIsActive()
    {
        var loan = CreateLoan();
        loan.Status.Should().Be(LoanStatus.Active);
    }

    [Fact]
    public void EmployeeLoan_Create_RepaymentsCollectionIsEmpty()
    {
        var loan = CreateLoan();
        loan.Repayments.Should().BeEmpty();
    }

    [Fact]
    public void EmployeeLoan_Create_PopulatesCoreProperties()
    {
        // Arrange
        var employeeId = new EmployeeId(Guid.NewGuid());

        // Act
        var loan = EmployeeLoan.Create(
            tenantId: "tenant-xyz",
            employeeId: employeeId,
            principalAmount: 75_000m,
            interestRate: 0.12m,
            tenureMonths: 24);

        // Assert
        loan.TenantId.Should().Be("tenant-xyz");
        loan.EmployeeId.Should().Be(employeeId);
        loan.PrincipalAmount.Should().Be(75_000m);
        loan.InterestRate.Should().Be(0.12m);
        loan.TenureMonths.Should().Be(24);
    }

    // ─── RecordRepayment ─────────────────────────────────────────────────────────

    [Fact]
    public void EmployeeLoan_RecordRepayment_DecreasesRemainingPrincipal()
    {
        // Arrange
        var loan = CreateLoan(principal: 100_000m);

        // Act
        loan.RecordRepayment(10_000m, "EMI month 1", NewChain());

        // Assert
        loan.RemainingPrincipal.Should().Be(90_000m);
    }

    [Fact]
    public void EmployeeLoan_RecordRepayment_AddsEntryToRepaymentsCollection()
    {
        // Arrange
        var loan = CreateLoan(principal: 100_000m);

        // Act
        loan.RecordRepayment(5_000m, "Partial repayment", NewChain());

        // Assert
        loan.Repayments.Should().HaveCount(1);
        loan.Repayments.First().Amount.Should().Be(5_000m);
        loan.Repayments.First().Explanation.Should().Be("Partial repayment");
    }

    [Fact]
    public void EmployeeLoan_RecordRepayment_MultipleRepayments_AccumulatesCorrectly()
    {
        // Arrange
        var loan = CreateLoan(principal: 60_000m);

        // Act
        loan.RecordRepayment(20_000m, "Month 1", NewChain());
        loan.RecordRepayment(20_000m, "Month 2", NewChain());

        // Assert
        loan.RemainingPrincipal.Should().Be(20_000m);
        loan.Repayments.Should().HaveCount(2);
    }

    [Fact]
    public void EmployeeLoan_RecordRepayment_WhenFullyRepaid_StatusBecomesClosed()
    {
        // Arrange
        var loan = CreateLoan(principal: 30_000m);

        // Act
        loan.RecordRepayment(30_000m, "Final repayment", NewChain());

        // Assert
        loan.Status.Should().Be(LoanStatus.Closed);
        loan.RemainingPrincipal.Should().Be(0m);
    }

    [Fact]
    public void EmployeeLoan_RecordRepayment_AmountExceedsPrincipal_CapsAtRemainingPrincipal()
    {
        // Arrange — the domain caps the repayment at remaining principal
        var loan = CreateLoan(principal: 10_000m);

        // Act
        loan.RecordRepayment(15_000m, "Overpayment attempt", NewChain());

        // Assert — remaining goes to 0, not negative
        loan.RemainingPrincipal.Should().Be(0m);
        loan.Repayments.First().Amount.Should().Be(10_000m); // capped
        loan.Status.Should().Be(LoanStatus.Closed);
    }

    [Fact]
    public void EmployeeLoan_RecordRepayment_WithZeroAmount_ThrowsArgumentException()
    {
        // Arrange
        var loan = CreateLoan();

        // Act
        var act = () => loan.RecordRepayment(0m, "Zero payment", NewChain());

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*positive*");
    }

    [Fact]
    public void EmployeeLoan_RecordRepayment_WithNegativeAmount_ThrowsArgumentException()
    {
        // Arrange
        var loan = CreateLoan();

        // Act
        var act = () => loan.RecordRepayment(-500m, "Negative", NewChain());

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*positive*");
    }

    [Fact]
    public void EmployeeLoan_RecordRepayment_EachEntryHasUniqueId()
    {
        // Arrange
        var loan = CreateLoan(principal: 90_000m);

        // Act
        loan.RecordRepayment(10_000m, "Month 1", NewChain());
        loan.RecordRepayment(10_000m, "Month 2", NewChain());
        loan.RecordRepayment(10_000m, "Month 3", NewChain());

        // Assert
        var ids = loan.Repayments.Select(r => r.Id).ToList();
        ids.Should().OnlyHaveUniqueItems();
    }

    // ─── Full lifecycle ───────────────────────────────────────────────────────────

    [Fact]
    public void EmployeeLoan_FullLifecycle_ActiveToClosed_ThreeMonthRepaymentPlan()
    {
        // Arrange
        var loan = CreateLoan(principal: 30_000m, tenureMonths: 3);
        loan.Status.Should().Be(LoanStatus.Active);

        // Act — three equal monthly payments
        loan.RecordRepayment(10_000m, "Month 1 EMI", NewChain());
        loan.Status.Should().Be(LoanStatus.Active);
        loan.RemainingPrincipal.Should().Be(20_000m);

        loan.RecordRepayment(10_000m, "Month 2 EMI", NewChain());
        loan.Status.Should().Be(LoanStatus.Active);
        loan.RemainingPrincipal.Should().Be(10_000m);

        loan.RecordRepayment(10_000m, "Month 3 EMI", NewChain());

        // Assert final state
        loan.Status.Should().Be(LoanStatus.Closed);
        loan.RemainingPrincipal.Should().Be(0m);
        loan.Repayments.Should().HaveCount(3);
    }
}
