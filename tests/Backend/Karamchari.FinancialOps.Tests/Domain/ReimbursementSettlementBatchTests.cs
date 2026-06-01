using FluentAssertions;
using Karamchari.FinancialOps.Contracts.Primitives;
using Karamchari.FinancialOps.Contracts.Reimbursements;
using Karamchari.FinancialOps.Contracts.Settlements;
using Karamchari.FinancialOps.Domain.Settlements;
using Xunit;

namespace Karamchari.FinancialOps.Tests.Domain;

public sealed class ReimbursementSettlementBatchTests
{
    // ─── Helpers ────────────────────────────────────────────────────────────────

    private static ReimbursementSettlementBatch CreateBatch(
        string tenantId = "tenant-1",
        int claimCount = 2)
    {
        var claimIds = Enumerable.Range(0, claimCount)
            .Select(_ => ExpenseClaimId.New())
            .ToList();

        return ReimbursementSettlementBatch.Create(
            tenantId: tenantId,
            periodId: FinancialOperationalPeriodId.New(),
            type: SettlementType.DirectBankTransfer,
            claimIds: claimIds);
    }

    // ─── Create ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Batch_Create_StatusIsDraft()
    {
        var batch = CreateBatch();
        batch.Status.Should().Be(SettlementBatchStatus.Draft);
    }

    [Fact]
    public void Batch_Create_ClaimIdsArePopulated()
    {
        var claimIds = new[] { ExpenseClaimId.New(), ExpenseClaimId.New(), ExpenseClaimId.New() };

        var batch = ReimbursementSettlementBatch.Create(
            tenantId: "tenant-1",
            periodId: FinancialOperationalPeriodId.New(),
            type: SettlementType.Payroll,
            claimIds: claimIds);

        batch.ClaimIds.Should().HaveCount(3);
        batch.ClaimIds.Should().BeEquivalentTo(claimIds);
    }

    [Fact]
    public void Batch_Create_WithEmptyClaimIds_ThrowsArgumentException()
    {
        var act = () => ReimbursementSettlementBatch.Create(
            tenantId: "tenant-1",
            periodId: FinancialOperationalPeriodId.New(),
            type: SettlementType.Cash,
            claimIds: []);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*at least one claim*");
    }

    [Fact]
    public void Batch_Create_ExternalReferenceIsNull()
    {
        var batch = CreateBatch();
        batch.ExternalReference.Should().BeNull();
    }

    [Fact]
    public void Batch_Create_FinalizedAtIsNull()
    {
        var batch = CreateBatch();
        batch.FinalizedAt.Should().BeNull();
    }

    // ─── StartExecution ──────────────────────────────────────────────────────────

    [Fact]
    public void Batch_StartExecution_FromDraft_SetsStatusToExecuting()
    {
        // Arrange
        var batch = CreateBatch();

        // Act
        batch.StartExecution("Initiated by scheduler");

        // Assert
        batch.Status.Should().Be(SettlementBatchStatus.Executing);
    }

    [Fact]
    public void Batch_StartExecution_SetsExecutedAt()
    {
        // Arrange
        var batch = CreateBatch();
        var before = DateTimeOffset.UtcNow;

        // Act
        batch.StartExecution("Starting payout run");

        // Assert
        batch.ExecutedAt.Should().NotBeNull();
        batch.ExecutedAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void Batch_StartExecution_SetsLastExplanation()
    {
        // Arrange
        var batch = CreateBatch();

        // Act
        batch.StartExecution("Batch initiated via scheduler");

        // Assert
        batch.LastExplanation.Should().Be("Batch initiated via scheduler");
    }

    [Fact]
    public void Batch_StartExecution_WhenAlreadyExecuting_ThrowsInvalidOperationException()
    {
        // Arrange
        var batch = CreateBatch();
        batch.StartExecution("First start");

        // Act
        var act = () => batch.StartExecution("Second start");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{SettlementBatchStatus.Executing}*");
    }

    [Fact]
    public void Batch_StartExecution_WhenFinalized_ThrowsInvalidOperationException()
    {
        // Arrange
        var batch = CreateBatch();
        batch.StartExecution("start");
        batch.Complete("EXT-001", "done");
        batch.FinalizeBatch("finalized");

        // Act
        var act = () => batch.StartExecution("Restarting finalized batch");

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    // ─── Complete ────────────────────────────────────────────────────────────────

    [Fact]
    public void Batch_Complete_WhenExecuting_SetsStatusToCompleted()
    {
        // Arrange
        var batch = CreateBatch();
        batch.StartExecution("Starting");

        // Act
        batch.Complete("EXT-REF-12345", "Payout confirmed");

        // Assert
        batch.Status.Should().Be(SettlementBatchStatus.Completed);
    }

    [Fact]
    public void Batch_Complete_SetsExternalReference()
    {
        // Arrange
        var batch = CreateBatch();
        batch.StartExecution("Starting");

        // Act
        batch.Complete("BANK-TXN-9876", "Provider confirmed");

        // Assert
        batch.ExternalReference.Should().Be("BANK-TXN-9876");
    }

    [Fact]
    public void Batch_Complete_WhenNotExecuting_ThrowsInvalidOperationException()
    {
        // Arrange — batch is still in Draft
        var batch = CreateBatch();

        // Act
        var act = () => batch.Complete("EXT-001", "Completed");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*executing*");
    }

    // ─── FinalizeBatch ────────────────────────────────────────────────────────────

    [Fact]
    public void Batch_FinalizeBatch_WhenCompleted_SetsStatusToFinalized()
    {
        // Arrange
        var batch = CreateBatch();
        batch.StartExecution("start");
        batch.Complete("EXT-001", "done");

        // Act
        batch.FinalizeBatch("Reconciliation complete");

        // Assert
        batch.Status.Should().Be(SettlementBatchStatus.Finalized);
    }

    [Fact]
    public void Batch_FinalizeBatch_SetsFinalizedAt()
    {
        // Arrange
        var batch = CreateBatch();
        batch.StartExecution("start");
        batch.Complete("EXT-001", "done");
        var before = DateTimeOffset.UtcNow;

        // Act
        batch.FinalizeBatch("Reconciliation complete");

        // Assert
        batch.FinalizedAt.Should().NotBeNull();
        batch.FinalizedAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void Batch_FinalizeBatch_WhenStillDraft_ThrowsInvalidOperationException()
    {
        // Arrange
        var batch = CreateBatch();

        // Act
        var act = () => batch.FinalizeBatch("Trying to finalize too early");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*completed or reconciled*");
    }

    [Fact]
    public void Batch_FinalizeBatch_WhenExecuting_ThrowsInvalidOperationException()
    {
        // Arrange
        var batch = CreateBatch();
        batch.StartExecution("start");

        // Act
        var act = () => batch.FinalizeBatch("Premature finalization");

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    // ─── Full lifecycle ───────────────────────────────────────────────────────────

    [Fact]
    public void Batch_FullLifecycle_DraftToFinalized_AllTransitionsSucceed()
    {
        // Arrange
        var batch = CreateBatch(claimCount: 5);

        // Assert initial state
        batch.Status.Should().Be(SettlementBatchStatus.Draft);
        batch.ClaimIds.Should().HaveCount(5);

        // Act & Assert each transition
        batch.StartExecution("Scheduler triggered");
        batch.Status.Should().Be(SettlementBatchStatus.Executing);
        batch.ExecutedAt.Should().NotBeNull();

        batch.Complete("TXN-REF-98765", "Bank confirmed");
        batch.Status.Should().Be(SettlementBatchStatus.Completed);
        batch.ExternalReference.Should().Be("TXN-REF-98765");

        batch.FinalizeBatch("Month-end reconciliation");
        batch.Status.Should().Be(SettlementBatchStatus.Finalized);
        batch.FinalizedAt.Should().NotBeNull();
    }
}
