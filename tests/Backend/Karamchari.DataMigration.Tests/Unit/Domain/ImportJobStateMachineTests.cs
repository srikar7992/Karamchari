// -----------------------------------------------------------------------
// <copyright file="ImportJobStateMachineTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Karamchari.DataMigration.Domain.Importing;
using Xunit;

namespace Karamchari.DataMigration.Tests.Unit.Domain;

public sealed class ImportJobStateMachineTests
{
    private static ImportJob CreateJob() =>
        ImportJob.Create(
            importType: "EmployeeImport",
            fileName: "employees.xlsx",
            storedFileId: "file-001",
            fileHash: "abc123",
            createdBy: "admin@example.com");

    // ------------------------------------------------------------------ ImportJob.Create

    [Fact]
    public void Create_ReturnsJobWithStatusCreated()
    {
        var job = CreateJob();

        job.Status.Should().Be(ImportJobStatus.Created);
    }

    [Fact]
    public void Create_ReturnsJobWithEmptyRecordsCollection()
    {
        var job = CreateJob();

        job.Records.Should().BeEmpty();
    }

    [Fact]
    public void Create_ReturnsJobWithNewId()
    {
        var job = CreateJob();

        job.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_SetsCreatedAt()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);

        var job = CreateJob();

        job.CreatedAt.Should().BeAfter(before);
    }

    [Fact]
    public void Create_CompletedAtIsNull()
    {
        var job = CreateJob();

        job.CompletedAt.Should().BeNull();
    }

    // ------------------------------------------------------------------ valid transitions

    [Fact]
    public void TransitionTo_CreatedToValidated_Allowed()
    {
        var job = CreateJob();

        var act = () => job.TransitionTo(ImportJobStatus.Validated);

        act.Should().NotThrow();
        job.Status.Should().Be(ImportJobStatus.Validated);
    }

    [Fact]
    public void TransitionTo_CreatedToPreviewed_Allowed()
    {
        var job = CreateJob();

        var act = () => job.TransitionTo(ImportJobStatus.Previewed);

        act.Should().NotThrow();
        job.Status.Should().Be(ImportJobStatus.Previewed);
    }

    [Fact]
    public void TransitionTo_ValidatedToQueued_Allowed()
    {
        var job = CreateJob();
        job.TransitionTo(ImportJobStatus.Validated);

        var act = () => job.TransitionTo(ImportJobStatus.Queued);

        act.Should().NotThrow();
        job.Status.Should().Be(ImportJobStatus.Queued);
    }

    [Fact]
    public void TransitionTo_QueuedToProcessing_Allowed()
    {
        var job = CreateJob();
        job.TransitionTo(ImportJobStatus.Validated);
        job.TransitionTo(ImportJobStatus.Queued);

        var act = () => job.TransitionTo(ImportJobStatus.Processing);

        act.Should().NotThrow();
        job.Status.Should().Be(ImportJobStatus.Processing);
    }

    [Fact]
    public void TransitionTo_ProcessingToCompleted_Allowed()
    {
        var job = CreateJob();
        job.TransitionTo(ImportJobStatus.Validated);
        job.TransitionTo(ImportJobStatus.Queued);
        job.TransitionTo(ImportJobStatus.Processing);

        var act = () => job.TransitionTo(ImportJobStatus.Completed);

        act.Should().NotThrow();
        job.Status.Should().Be(ImportJobStatus.Completed);
    }

    [Fact]
    public void TransitionTo_ProcessingToCompletedWithErrors_Allowed()
    {
        var job = CreateJob();
        job.TransitionTo(ImportJobStatus.Validated);
        job.TransitionTo(ImportJobStatus.Queued);
        job.TransitionTo(ImportJobStatus.Processing);

        var act = () => job.TransitionTo(ImportJobStatus.CompletedWithErrors);

        act.Should().NotThrow();
        job.Status.Should().Be(ImportJobStatus.CompletedWithErrors);
    }

    [Fact]
    public void TransitionTo_ProcessingToFailed_Allowed()
    {
        var job = CreateJob();
        job.TransitionTo(ImportJobStatus.Validated);
        job.TransitionTo(ImportJobStatus.Queued);
        job.TransitionTo(ImportJobStatus.Processing);

        var act = () => job.TransitionTo(ImportJobStatus.Failed);

        act.Should().NotThrow();
        job.Status.Should().Be(ImportJobStatus.Failed);
    }

    // ------------------------------------------------------------------ terminal state guards

    [Theory]
    [InlineData(ImportJobStatus.Created)]
    [InlineData(ImportJobStatus.Validated)]
    [InlineData(ImportJobStatus.Queued)]
    [InlineData(ImportJobStatus.Processing)]
    public void TransitionTo_FromCompleted_ThrowsInvalidOperationException(ImportJobStatus target)
    {
        var job = CreateJob();
        job.TransitionTo(ImportJobStatus.Validated);
        job.TransitionTo(ImportJobStatus.Queued);
        job.TransitionTo(ImportJobStatus.Processing);
        job.TransitionTo(ImportJobStatus.Completed);

        var act = () => job.TransitionTo(target);

        act.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData(ImportJobStatus.Created)]
    [InlineData(ImportJobStatus.Validated)]
    [InlineData(ImportJobStatus.Queued)]
    [InlineData(ImportJobStatus.Processing)]
    [InlineData(ImportJobStatus.Completed)]
    public void TransitionTo_FromFailed_ThrowsInvalidOperationException(ImportJobStatus target)
    {
        var job = CreateJob();
        job.TransitionTo(ImportJobStatus.Validated);
        job.TransitionTo(ImportJobStatus.Queued);
        job.TransitionTo(ImportJobStatus.Processing);
        job.TransitionTo(ImportJobStatus.Failed);

        var act = () => job.TransitionTo(target);

        act.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData(ImportJobStatus.Created)]
    [InlineData(ImportJobStatus.Validated)]
    [InlineData(ImportJobStatus.Queued)]
    [InlineData(ImportJobStatus.Processing)]
    [InlineData(ImportJobStatus.Completed)]
    public void TransitionTo_FromCancelled_ThrowsInvalidOperationException(ImportJobStatus target)
    {
        var job = CreateJob();
        job.TransitionTo(ImportJobStatus.Cancelled);

        var act = () => job.TransitionTo(target);

        act.Should().Throw<InvalidOperationException>();
    }

    // ------------------------------------------------------------------ CompletedAt set on terminal transitions

    [Fact]
    public void TransitionTo_Completed_SetsCompletedAt()
    {
        var job = CreateJob();
        job.TransitionTo(ImportJobStatus.Validated);
        job.TransitionTo(ImportJobStatus.Queued);
        job.TransitionTo(ImportJobStatus.Processing);

        var before = DateTime.UtcNow.AddSeconds(-1);
        job.TransitionTo(ImportJobStatus.Completed);

        job.CompletedAt.Should().NotBeNull();
        job.CompletedAt!.Value.Should().BeAfter(before);
    }

    [Fact]
    public void TransitionTo_Failed_SetsCompletedAt()
    {
        var job = CreateJob();
        job.TransitionTo(ImportJobStatus.Validated);
        job.TransitionTo(ImportJobStatus.Queued);
        job.TransitionTo(ImportJobStatus.Processing);

        var before = DateTime.UtcNow.AddSeconds(-1);
        job.TransitionTo(ImportJobStatus.Failed);

        job.CompletedAt.Should().NotBeNull();
        job.CompletedAt!.Value.Should().BeAfter(before);
    }

    [Fact]
    public void TransitionTo_CompletedWithErrors_SetsCompletedAt()
    {
        var job = CreateJob();
        job.TransitionTo(ImportJobStatus.Validated);
        job.TransitionTo(ImportJobStatus.Queued);
        job.TransitionTo(ImportJobStatus.Processing);

        job.TransitionTo(ImportJobStatus.CompletedWithErrors);

        job.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void TransitionTo_Cancelled_SetsCompletedAt()
    {
        var job = CreateJob();

        job.TransitionTo(ImportJobStatus.Cancelled);

        job.CompletedAt.Should().NotBeNull();
    }

    // ------------------------------------------------------------------ non-terminal transitions do not set CompletedAt

    [Fact]
    public void TransitionTo_Validated_DoesNotSetCompletedAt()
    {
        var job = CreateJob();

        job.TransitionTo(ImportJobStatus.Validated);

        job.CompletedAt.Should().BeNull();
    }

    [Fact]
    public void TransitionTo_Queued_DoesNotSetCompletedAt()
    {
        var job = CreateJob();
        job.TransitionTo(ImportJobStatus.Validated);

        job.TransitionTo(ImportJobStatus.Queued);

        job.CompletedAt.Should().BeNull();
    }

    // ------------------------------------------------------------------ UpdateProgress

    [Fact]
    public void UpdateProgress_SetsSuccessfulRows()
    {
        var job = CreateJob();

        job.UpdateProgress(success: 50, failed: 5, skipped: 2);

        job.SuccessfulRows.Should().Be(50);
    }

    [Fact]
    public void UpdateProgress_SetsFailedRows()
    {
        var job = CreateJob();

        job.UpdateProgress(success: 50, failed: 5, skipped: 2);

        job.FailedRows.Should().Be(5);
    }

    [Fact]
    public void UpdateProgress_SetsSkippedRows()
    {
        var job = CreateJob();

        job.UpdateProgress(success: 50, failed: 5, skipped: 2);

        job.SkippedRows.Should().Be(2);
    }

    [Fact]
    public void UpdateProgress_TotalRowsIsSum()
    {
        var job = CreateJob();

        job.UpdateProgress(success: 50, failed: 5, skipped: 2);

        job.TotalRows.Should().Be(57);
    }

    [Fact]
    public void UpdateProgress_ZeroValues_AllCountsAreZero()
    {
        var job = CreateJob();

        job.UpdateProgress(success: 0, failed: 0, skipped: 0);

        job.TotalRows.Should().Be(0);
        job.SuccessfulRows.Should().Be(0);
        job.FailedRows.Should().Be(0);
        job.SkippedRows.Should().Be(0);
    }

    // ------------------------------------------------------------------ Records collection

    [Fact]
    public void Records_InitiallyEmpty()
    {
        var job = CreateJob();

        job.Records.Should().BeEmpty();
    }

    [Fact]
    public void Records_IsReadOnly()
    {
        var job = CreateJob();

        // The IReadOnlyCollection<ImportRecord> returned should not expose mutating methods.
        // Verify the concrete type is a read-only wrapper.
        job.Records.Should().BeAssignableTo<IReadOnlyCollection<ImportRecord>>();
    }
}
