// -----------------------------------------------------------------------
// <copyright file="ExpenseClaimTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Karamchari.FinancialOps.Contracts.Primitives;
using Karamchari.FinancialOps.Contracts.Reimbursements;
using Karamchari.FinancialOps.Contracts.Settlements;
using Karamchari.FinancialOps.Domain.Reimbursements;
using Xunit;

namespace Karamchari.FinancialOps.Tests.Domain;

public sealed class ExpenseClaimTests
{
    // ─── Helpers ────────────────────────────────────────────────────────────────

    private static ExpenseClaim CreateSubmittedClaim(
        string tenantId = "tenant-1",
        decimal amount = 1_500.00m)
    {
        return ExpenseClaim.Submit(
            tenantId: tenantId,
            employeeId: new EmployeeId(Guid.NewGuid()),
            categoryId: new ExpenseCategoryId(Guid.NewGuid()),
            claimNumber: $"CLM-{Guid.NewGuid():N}",
            claimedAmount: amount,
            currency: CurrencyCode.INR,
            expenseDate: DateOnly.FromDateTime(DateTime.Today),
            policyVersion: 1,
            taxTreatment: TaxTreatment.FullyExempt,
            settlementType: SettlementType.DirectBankTransfer);
    }

    // ─── Submit ──────────────────────────────────────────────────────────────────

    [Fact]
    public void ExpenseClaim_Submit_SetsStatusToSubmitted()
    {
        // Act
        var claim = CreateSubmittedClaim();

        // Assert
        claim.Status.Should().Be(ExpenseClaimStatus.Submitted);
    }

    [Fact]
    public void ExpenseClaim_Submit_PopulatesAllCoreProperties()
    {
        // Arrange
        var employeeId = new EmployeeId(Guid.NewGuid());
        var categoryId = new ExpenseCategoryId(Guid.NewGuid());
        const string claimNumber = "CLM-001";
        const decimal amount = 2_500.00m;

        // Act
        var claim = ExpenseClaim.Submit(
            tenantId: "tenant-abc",
            employeeId: employeeId,
            categoryId: categoryId,
            claimNumber: claimNumber,
            claimedAmount: amount,
            currency: CurrencyCode.INR,
            expenseDate: new DateOnly(2026, 5, 15),
            policyVersion: 3,
            taxTreatment: TaxTreatment.FullyExempt,
            settlementType: SettlementType.Payroll);

        // Assert
        claim.TenantId.Should().Be("tenant-abc");
        claim.EmployeeId.Should().Be(employeeId);
        claim.CategoryId.Should().Be(categoryId);
        claim.ClaimNumber.Should().Be(claimNumber);
        claim.ClaimedAmount.Should().Be(amount);
        claim.PolicyVersion.Should().Be(3);
        claim.Currency.Should().Be(CurrencyCode.INR);
        claim.TaxTreatment.Should().Be(TaxTreatment.FullyExempt);
        claim.PreferredSettlementType.Should().Be(SettlementType.Payroll);
        claim.Holds.Should().BeEmpty();
        claim.Receipts.Should().BeEmpty();
    }

    // ─── PlaceHold ───────────────────────────────────────────────────────────────

    [Fact]
    public void ExpenseClaim_PlaceHold_AddsHoldToCollection()
    {
        // Arrange
        var claim = CreateSubmittedClaim();

        // Act
        claim.PlaceHold(FinancialOperationalHoldType.Audit, "Routine audit", "manager-1");

        // Assert
        var hold = claim.Holds.Single();
        claim.Holds.Should().HaveCount(1);
        hold.Type.Should().Be(FinancialOperationalHoldType.Audit);
        hold.Reason.Should().Be("Routine audit");
        hold.PlacedBy.Should().Be("manager-1");
        hold.IsActive.Should().BeTrue();
    }

    [Fact]
    public void ExpenseClaim_PlaceHold_MultipleHolds_AllAddedToCollection()
    {
        // Arrange
        var claim = CreateSubmittedClaim();

        // Act
        claim.PlaceHold(FinancialOperationalHoldType.Audit, "Audit check", "manager-1");
        claim.PlaceHold(FinancialOperationalHoldType.Compliance, "Compliance flag", "compliance-team");

        // Assert
        claim.Holds.Should().HaveCount(2);
        claim.Holds.Where(h => h.IsActive).Should().HaveCount(2);
    }

    [Fact]
    public void ExpenseClaim_PlaceHold_OnSettledClaim_ThrowsInvalidOperationException()
    {
        // Arrange
        var claim = CreateSubmittedClaim();
        claim.Approve();
        claim.QueueForSettlement();
        claim.RecordSettlement();

        // Act
        var act = () => claim.PlaceHold(FinancialOperationalHoldType.Audit, "Late audit", "auditor");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*settled*");
    }

    // ─── ReleaseHold ─────────────────────────────────────────────────────────────

    [Fact]
    public void ExpenseClaim_ReleaseHold_RemovesActiveHoldFromCollection()
    {
        // Arrange
        var claim = CreateSubmittedClaim();
        claim.PlaceHold(FinancialOperationalHoldType.Audit, "Audit", "manager-1");

        // Act
        claim.ReleaseHold(FinancialOperationalHoldType.Audit, "manager-1");

        // Assert — the released hold is still in Holds but inactive
        var releasedHold = claim.Holds.Single();
        claim.Holds.Should().HaveCount(1);
        releasedHold.IsActive.Should().BeFalse();
        releasedHold.ReleasedBy.Should().Be("manager-1");
        releasedHold.ReleasedAt.Should().NotBeNull();
    }

    [Fact]
    public void ExpenseClaim_ReleaseHold_AllHoldsReleased_AllHoldsAreInactive()
    {
        // Arrange
        var claim = CreateSubmittedClaim();
        claim.PlaceHold(FinancialOperationalHoldType.Audit, "Audit", "manager-1");
        claim.PlaceHold(FinancialOperationalHoldType.Compliance, "Compliance", "compliance-team");

        // Act
        claim.ReleaseHold(FinancialOperationalHoldType.Audit, "manager-1");
        claim.ReleaseHold(FinancialOperationalHoldType.Compliance, "compliance-team");

        // Assert — no active holds remain
        claim.Holds.Where(h => h.IsActive).Should().BeEmpty();
    }

    [Fact]
    public void ExpenseClaim_ReleaseHold_NonExistentType_DoesNotThrow()
    {
        // Arrange
        var claim = CreateSubmittedClaim();

        // Act — releasing a hold that was never placed should be a no-op
        var act = () => claim.ReleaseHold(FinancialOperationalHoldType.SuspiciousActivity, "manager-1");

        // Assert
        act.Should().NotThrow();
        claim.Holds.Should().BeEmpty();
    }

    // ─── Approve ─────────────────────────────────────────────────────────────────

    [Fact]
    public void ExpenseClaim_Approve_WhenNoHolds_SetsStatusToApproved()
    {
        // Arrange
        var claim = CreateSubmittedClaim();

        // Act
        claim.Approve();

        // Assert
        claim.Status.Should().Be(ExpenseClaimStatus.Approved);
    }

    [Fact]
    public void ExpenseClaim_Approve_WhenActiveHoldsExist_ThrowsInvalidOperationException()
    {
        // Arrange
        var claim = CreateSubmittedClaim();
        claim.PlaceHold(FinancialOperationalHoldType.Audit, "Audit", "manager-1");

        // Act
        var act = () => claim.Approve();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*active operational holds*");
    }

    [Fact]
    public void ExpenseClaim_Approve_AfterAllHoldsReleased_SetsStatusToApproved()
    {
        // Arrange
        var claim = CreateSubmittedClaim();
        claim.PlaceHold(FinancialOperationalHoldType.Audit, "Audit", "manager-1");
        claim.ReleaseHold(FinancialOperationalHoldType.Audit, "manager-1");

        // Act
        claim.Approve();

        // Assert
        claim.Status.Should().Be(ExpenseClaimStatus.Approved);
    }

    [Fact]
    public void ExpenseClaim_Approve_WhenAlreadySettled_ThrowsInvalidOperationException()
    {
        // Arrange
        var claim = CreateSubmittedClaim();
        claim.Approve();
        claim.QueueForSettlement();
        claim.RecordSettlement();

        // Act
        var act = () => claim.Approve();

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    // ─── QueueForSettlement ───────────────────────────────────────────────────────

    [Fact]
    public void ExpenseClaim_QueueForSettlement_WhenApproved_SetsStatusToQueuedForSettlement()
    {
        // Arrange
        var claim = CreateSubmittedClaim();
        claim.Approve();

        // Act
        claim.QueueForSettlement();

        // Assert
        claim.Status.Should().Be(ExpenseClaimStatus.QueuedForSettlement);
    }

    [Fact]
    public void ExpenseClaim_QueueForSettlement_WhenNotApproved_ThrowsInvalidOperationException()
    {
        // Arrange — claim is still in Submitted state
        var claim = CreateSubmittedClaim();

        // Act
        var act = () => claim.QueueForSettlement();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*approved*");
    }

    [Fact]
    public void ExpenseClaim_QueueForSettlement_WhenAlreadyQueued_ThrowsInvalidOperationException()
    {
        // Arrange
        var claim = CreateSubmittedClaim();
        claim.Approve();
        claim.QueueForSettlement();

        // Act
        var act = () => claim.QueueForSettlement();

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    // ─── RecordSettlement ─────────────────────────────────────────────────────────

    [Fact]
    public void ExpenseClaim_RecordSettlement_WhenQueued_SetsStatusToSettled()
    {
        // Arrange
        var claim = CreateSubmittedClaim();
        claim.Approve();
        claim.QueueForSettlement();

        // Act
        claim.RecordSettlement();

        // Assert
        claim.Status.Should().Be(ExpenseClaimStatus.Settled);
    }

    [Fact]
    public void ExpenseClaim_RecordSettlement_WhenNotQueued_ThrowsInvalidOperationException()
    {
        // Arrange — claim is Approved but not yet queued
        var claim = CreateSubmittedClaim();
        claim.Approve();

        // Act
        var act = () => claim.RecordSettlement();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*queued*");
    }

    // ─── Full lifecycle ───────────────────────────────────────────────────────────

    [Fact]
    public void ExpenseClaim_FullLifecycle_SubmitToSettled_AllTransitionsSucceed()
    {
        // Arrange
        var claim = CreateSubmittedClaim(amount: 5_000.00m);

        // Act & Assert — each step
        claim.Status.Should().Be(ExpenseClaimStatus.Submitted);

        claim.PlaceHold(FinancialOperationalHoldType.SuspiciousActivity, "Flagged by fraud engine", "fraud-system");
        claim.Holds.Where(h => h.IsActive).Should().HaveCount(1);

        claim.ReleaseHold(FinancialOperationalHoldType.SuspiciousActivity, "fraud-reviewer");
        claim.Holds.Where(h => h.IsActive).Should().BeEmpty();

        claim.Approve();
        claim.Status.Should().Be(ExpenseClaimStatus.Approved);

        claim.QueueForSettlement();
        claim.Status.Should().Be(ExpenseClaimStatus.QueuedForSettlement);

        claim.RecordSettlement();
        claim.Status.Should().Be(ExpenseClaimStatus.Settled);
    }
}
