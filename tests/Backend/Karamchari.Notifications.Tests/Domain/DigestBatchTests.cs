// -----------------------------------------------------------------------
// <copyright file="DigestBatchTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Karamchari.Notifications.Domain;
using Xunit;

namespace Karamchari.Notifications.Tests.Domain;

public sealed class DigestBatchTests
{
    // ─── helpers ────────────────────────────────────────────────────────────

    private static DigestBatch BuildBatch(
        DigestType digestType = DigestType.Daily,
        string? windowKey = null)
    {
        return DigestBatch.Create(
            tenantId: "tenant-001",
            recipientEmployeeId: Guid.NewGuid(),
            recipientEmail: "employee@example.com",
            digestType: digestType,
            windowKey: windowKey ?? "2026-06-01",
            sourceMessageIds: [Guid.NewGuid(), Guid.NewGuid()],
            renderedSubject: "Your daily digest",
            renderedBodyHtml: "<p>2 notifications inside.</p>",
            renderedBodyText: "2 notifications inside.",
            scheduleDeliverAfter: null);
    }

    // ─── Create ──────────────────────────────────────────────────────────────

    [Fact]
    public void DigestBatch_Create_StatusIsPending()
    {
        var batch = BuildBatch();

        batch.Status.Should().Be(DigestBatchStatus.Pending);
    }

    [Fact]
    public void DigestBatch_Create_MessageCountMatchesSourceIds()
    {
        var sourceIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var batch = DigestBatch.Create(
            tenantId: "tenant-001",
            recipientEmployeeId: Guid.NewGuid(),
            recipientEmail: "employee@example.com",
            digestType: DigestType.Weekly,
            windowKey: "2026-W22",
            sourceMessageIds: sourceIds,
            renderedSubject: "Weekly digest",
            renderedBodyHtml: "<p>3 items</p>",
            renderedBodyText: "3 items");

        batch.MessageCount.Should().Be(3);
    }

    // ─── MarkDelivered ───────────────────────────────────────────────────────

    [Fact]
    public void DigestBatch_MarkDelivered_SetsStatus()
    {
        var batch = BuildBatch();
        var before = DateTimeOffset.UtcNow;

        batch.MarkDelivered();

        batch.Status.Should().Be(DigestBatchStatus.Delivered);
        batch.DeliveredOnUtc.Should().NotBeNull();
        batch.DeliveredOnUtc!.Value.Should().BeOnOrAfter(before);
    }

    // ─── MarkFailed ──────────────────────────────────────────────────────────

    [Fact]
    public void DigestBatch_MarkFailed_SetsStatusFailed()
    {
        var batch = BuildBatch();

        batch.MarkFailed("SMTP gateway unreachable");

        batch.Status.Should().Be(DigestBatchStatus.Failed);
    }

    // ─── Cancel ──────────────────────────────────────────────────────────────

    [Fact]
    public void DigestBatch_Cancel_SetsStatusCancelled()
    {
        var batch = BuildBatch();

        batch.Cancel();

        batch.Status.Should().Be(DigestBatchStatus.Cancelled);
    }
}
