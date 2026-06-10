// -----------------------------------------------------------------------
// <copyright file="NotificationMessageTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Karamchari.Notifications.Domain;
using Xunit;

namespace Karamchari.Notifications.Tests.Domain;

public sealed class NotificationMessageTests
{
    // ─── helpers ────────────────────────────────────────────────────────────

    private static NotificationMessage BuildMessage(
        string? idempotencyKey = null,
        DateTimeOffset? scheduleDeliverAfter = null)
    {
        return NotificationMessage.Create(
            tenantId: "tenant-001",
            recipientEmployeeId: Guid.NewGuid(),
            recipientEmail: "employee@example.com",
            category: NotificationCategory.ReviewWorkflow,
            preferredChannel: NotificationChannel.Email,
            templateId: Guid.NewGuid(),
            renderedSubject: "Your review is due",
            renderedBodyHtml: "<p>Please complete your review.</p>",
            renderedBodyText: "Please complete your review.",
            idempotencyKey: idempotencyKey ?? "tenant-001:event-1:ReviewWorkflow:emp-1",
            scheduleDeliverAfter: scheduleDeliverAfter);
    }

    // ─── Create ──────────────────────────────────────────────────────────────

    [Fact]
    public void NotificationMessage_Create_StatusIsPending()
    {
        var message = BuildMessage();

        message.Status.Should().Be(NotificationStatus.Pending);
    }

    [Fact]
    public void NotificationMessage_Create_IdempotencyKeyFormat_IsCorrect()
    {
        var tenantId = "tenant-abc";
        var eventId = Guid.NewGuid();
        var category = NotificationCategory.GoalManagement;
        var employeeId = Guid.NewGuid();
        var expectedKey = $"{tenantId}:{eventId}:{category}:{employeeId}";

        var message = NotificationMessage.Create(
            tenantId: tenantId,
            recipientEmployeeId: employeeId,
            recipientEmail: "emp@company.com",
            category: category,
            preferredChannel: NotificationChannel.InApp,
            templateId: Guid.NewGuid(),
            renderedSubject: "Goal update",
            renderedBodyHtml: "<p>Your goal was updated.</p>",
            renderedBodyText: "Your goal was updated.",
            idempotencyKey: expectedKey);

        message.IdempotencyKey.Should().Be(expectedKey);
        message.IdempotencyKey.Should().Contain(tenantId);
        message.IdempotencyKey.Should().Contain(category.ToString());
        message.IdempotencyKey.Should().Contain(employeeId.ToString());
    }

    // ─── RecordDeliveryAttempt ───────────────────────────────────────────────

    [Fact]
    public void NotificationMessage_RecordDeliveryAttempt_Success_SetsStatusDelivered()
    {
        var message = BuildMessage();

        message.RecordDeliveryAttempt(
            NotificationChannel.Email,
            DeliveryResult.Success,
            providerMessageId: "msg-provider-001",
            failureReason: null);

        message.Status.Should().Be(NotificationStatus.Delivered);
        message.DeliveredOnUtc.Should().NotBeNull();
        message.Attempts.Should().HaveCount(1);
    }

    [Fact]
    public void NotificationMessage_RecordDeliveryAttempt_PermanentFailure_SetsStatusFailed()
    {
        var message = BuildMessage();

        message.RecordDeliveryAttempt(
            NotificationChannel.Email,
            DeliveryResult.PermanentFailure,
            providerMessageId: null,
            failureReason: "Invalid email address");

        message.Status.Should().Be(NotificationStatus.Failed);
        message.DeliveredOnUtc.Should().BeNull();
    }

    [Fact]
    public void NotificationMessage_RecordDeliveryAttempt_TransientFailure_RemainsPending()
    {
        var message = BuildMessage();

        message.RecordDeliveryAttempt(
            NotificationChannel.Email,
            DeliveryResult.TransientFailure,
            providerMessageId: null,
            failureReason: "Upstream timeout");

        message.Status.Should().Be(NotificationStatus.Pending);
        message.DeliveredOnUtc.Should().BeNull();
        message.Attempts.Should().HaveCount(1);
    }

    // ─── Defer ───────────────────────────────────────────────────────────────

    [Fact]
    public void NotificationMessage_Defer_SetsStatusDeferred()
    {
        var message = BuildMessage();
        var deliverAfter = DateTimeOffset.UtcNow.AddHours(8);

        message.Defer(deliverAfter);

        message.Status.Should().Be(NotificationStatus.Deferred);
        message.ScheduleDeliverAfter.Should().Be(deliverAfter);
    }

    // ─── Cancel ──────────────────────────────────────────────────────────────

    [Fact]
    public void NotificationMessage_Cancel_SetsStatusCancelled()
    {
        var message = BuildMessage();

        message.Cancel();

        message.Status.Should().Be(NotificationStatus.Cancelled);
    }

    // ─── MarkRead ────────────────────────────────────────────────────────────

    [Fact]
    public void NotificationMessage_MarkRead_SetsIsReadTrueWithTimestamp()
    {
        var message = BuildMessage();
        var before = DateTimeOffset.UtcNow;

        message.MarkRead();

        message.IsRead.Should().BeTrue();
        message.ReadOnUtc.Should().NotBeNull();
        message.ReadOnUtc!.Value.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void NotificationMessage_MarkRead_WhenAlreadyRead_IsIdempotent()
    {
        var message = BuildMessage();
        message.MarkRead();
        var firstReadTime = message.ReadOnUtc;

        // Call MarkRead a second time — timestamp must not change
        message.MarkRead();

        message.IsRead.Should().BeTrue();
        message.ReadOnUtc.Should().Be(firstReadTime);
    }

    // ─── QueueForDigest ──────────────────────────────────────────────────────

    [Fact]
    public void NotificationMessage_QueueForDigest_SetsStatusDigestQueued()
    {
        var message = BuildMessage();

        message.QueueForDigest();

        message.Status.Should().Be(NotificationStatus.DigestQueued);
    }
}
