// -----------------------------------------------------------------------
// <copyright file="AttendanceNotificationConsumer.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Notifications.Domain;
using Karamchari.Notifications.Orchestration;
using Karamchari.TimeAttendance.Contracts;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Karamchari.Notifications.Consumers;

/// <summary>
/// Routes attendance integration events to the notification orchestrator.
/// Covers: geo fraud detection, consecutive absence escalation, regularization outcomes.
/// </summary>
public sealed class AttendanceNotificationConsumer :
    IConsumer<GeoFraudDetectedIntegrationEventV1>,
    IConsumer<ConsecutiveAbsenceEscalatedIntegrationEventV1>,
    IConsumer<AttendanceExceptionRaisedIntegrationEventV1>,
    IConsumer<RegularizationApprovedIntegrationEventV1>,
    IConsumer<RegularizationRejectedIntegrationEventV1>
{
    private readonly INotificationOrchestrator _notifications;
    private readonly ILogger<AttendanceNotificationConsumer> _logger;

    public AttendanceNotificationConsumer(
        INotificationOrchestrator notifications,
        ILogger<AttendanceNotificationConsumer> logger)
    {
        _notifications = notifications;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<GeoFraudDetectedIntegrationEventV1> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var msg = context.Message;

        _logger.LogWarning(
            "GeoFraud escalated for employee {EmployeeId}. Type={FraudType} Severity={Severity}",
            msg.EmployeeId, msg.FraudType, msg.Severity);

        await _notifications.OrchestrateAsync(new NotificationIntent(
            msg.TenantId,
            context.MessageId ?? Guid.NewGuid(),
            NotificationIntentType.AttendanceGeoFraudDetected,
            NotificationCategory.Attendance,
            msg.EmployeeId,
            $"employee_{msg.EmployeeId}@karamchari.com",
            "en-IN",
            new Dictionary<string, string>
            {
                ["fraudType"] = msg.FraudType,
                ["severity"] = msg.Severity,
                ["description"] = msg.Description,
                ["detectedAt"] = msg.DetectedAtUtc.ToString("yyyy-MM-dd HH:mm UTC"),
            }), context.CancellationToken);
    }

    public async Task Consume(ConsumeContext<ConsecutiveAbsenceEscalatedIntegrationEventV1> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var msg = context.Message;

        _logger.LogWarning(
            "Consecutive absence escalated for employee {EmployeeId}. Days={Days} From={From} To={To}",
            msg.EmployeeId, msg.ConsecutiveDays, msg.FirstAbsentDate, msg.LastAbsentDate);

        await _notifications.OrchestrateAsync(new NotificationIntent(
            msg.TenantId,
            context.MessageId ?? Guid.NewGuid(),
            NotificationIntentType.AttendanceConsecutiveAbsenceEscalated,
            NotificationCategory.Attendance,
            msg.EmployeeId,
            $"employee_{msg.EmployeeId}@karamchari.com",
            "en-IN",
            new Dictionary<string, string>
            {
                ["consecutiveDays"] = msg.ConsecutiveDays.ToString(),
                ["firstAbsentDate"] = msg.FirstAbsentDate.ToString("yyyy-MM-dd"),
                ["lastAbsentDate"] = msg.LastAbsentDate.ToString("yyyy-MM-dd"),
            }), context.CancellationToken);
    }

    public async Task Consume(ConsumeContext<AttendanceExceptionRaisedIntegrationEventV1> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var msg = context.Message;

        var intentType = msg.ExceptionType switch
        {
            "LateArrival" => NotificationIntentType.AttendanceLateArrival,
            "NoShow" => NotificationIntentType.AttendanceNoShow,
            "MissingCheckOut" => NotificationIntentType.AttendanceMissingCheckout,
            _ => (NotificationIntentType?)null,
        };

        if (intentType is null)
        {
            _logger.LogDebug(
                "No notification intent mapped for exception type {ExceptionType}. Skipping.",
                msg.ExceptionType);
            return;
        }

        _logger.LogInformation(
            "Attendance exception raised for employee {EmployeeId}. Type={Type} Severity={Severity}",
            msg.EmployeeId, msg.ExceptionType, msg.Severity);

        await _notifications.OrchestrateAsync(new NotificationIntent(
            msg.TenantId,
            context.MessageId ?? Guid.NewGuid(),
            intentType.Value,
            NotificationCategory.Attendance,
            msg.EmployeeId,
            $"employee_{msg.EmployeeId}@karamchari.com",
            "en-IN",
            new Dictionary<string, string>
            {
                ["exceptionType"] = msg.ExceptionType,
                ["severity"] = msg.Severity,
                ["description"] = msg.Description,
                ["raisedAt"] = msg.RaisedAtUtc.ToString("yyyy-MM-dd HH:mm UTC"),
            }), context.CancellationToken);
    }

    public async Task Consume(ConsumeContext<RegularizationApprovedIntegrationEventV1> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var msg = context.Message;

        _logger.LogInformation(
            "Regularization approved for employee {EmployeeId}. RequestId={RequestId}",
            msg.EmployeeId, msg.RegularizationRequestId);

        await _notifications.OrchestrateAsync(new NotificationIntent(
            msg.TenantId,
            context.MessageId ?? Guid.NewGuid(),
            NotificationIntentType.AttendanceRegularizationApproved,
            NotificationCategory.Attendance,
            msg.EmployeeId,
            $"employee_{msg.EmployeeId}@karamchari.com",
            "en-IN",
            new Dictionary<string, string>
            {
                ["requestId"] = msg.RegularizationRequestId.ToString(),
                ["approvedAt"] = msg.ApprovedAtUtc.ToString("yyyy-MM-dd HH:mm UTC"),
            }), context.CancellationToken);
    }

    public async Task Consume(ConsumeContext<RegularizationRejectedIntegrationEventV1> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var msg = context.Message;

        _logger.LogInformation(
            "Regularization rejected for employee {EmployeeId}. RequestId={RequestId} Reason={Reason}",
            msg.EmployeeId, msg.RegularizationRequestId, msg.Reason);

        await _notifications.OrchestrateAsync(new NotificationIntent(
            msg.TenantId,
            context.MessageId ?? Guid.NewGuid(),
            NotificationIntentType.AttendanceRegularizationRejected,
            NotificationCategory.Attendance,
            msg.EmployeeId,
            $"employee_{msg.EmployeeId}@karamchari.com",
            "en-IN",
            new Dictionary<string, string>
            {
                ["requestId"] = msg.RegularizationRequestId.ToString(),
                ["reason"] = msg.Reason,
                ["rejectedAt"] = msg.RejectedAtUtc.ToString("yyyy-MM-dd HH:mm UTC"),
            }), context.CancellationToken);
    }
}
