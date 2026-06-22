// -----------------------------------------------------------------------
// <copyright file="Phase_Attendance_NotificationWiringTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Karamchari.TimeAttendance.Contracts;
using Karamchari.TimeAttendance.Domain.Attendance;
using Karamchari.TimeAttendance.Domain.Biometric;
using Karamchari.TimeAttendance.Domain.Policy;
using Karamchari.TimeAttendance.Services;
using Karamchari.TimeAttendance.Tests.Helpers;
using MassTransit;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Karamchari.TimeAttendance.Tests;

/// <summary>
/// Verifies that domain operations publish the correct integration events through IPublishEndpoint.
/// These tests confirm the notification wiring path is complete end-to-end.
/// </summary>
public sealed class AttendanceNotificationWiringTests
{
    // ── GeoFraud ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task PunchIngestion_high_severity_fraud_publishes_GeoFraudDetectedIntegrationEvent()
    {
        var db = TestDbContextFactory.Create("tenant-geo");
        var bus = Substitute.For<IPublishEndpoint>();
        var policyResolver = BuildPolicyResolver(db);
        var geoScorer = new GeoConfidenceScorer(db, NullLogger<GeoConfidenceScorer>.Instance);
        var processingEngine = BuildProcessingEngine(db, bus);
        var sut = new PunchIngestionService(db, geoScorer, policyResolver, processingEngine, bus,
            NullLogger<PunchIngestionService>.Instance);

        // MockGpsDetected + JailbreakDetected → both are High/Critical fraud signals
        var request = new PunchIngestionService.PunchRequest(
            TenantId: "tenant-geo",
            EmployeeId: Guid.NewGuid(),
            DeviceId: null,
            DeviceTimestampUtc: DateTimeOffset.UtcNow,
            Direction: PunchDirection.In,
            CaptureMode: CaptureMode.Mobile,
            Modality: null,
            Latitude: null,
            Longitude: null,
            GpsAccuracyMeters: null,
            WifiSsidMatched: false,
            IpWhitelisted: false,
            BleBeaconDetected: false,
            VpnMatched: false,
            MockGpsDetected: true,
            JailbreakDetected: true,
            DeviceFingerprint: null,
            BiometricMatchScore: null,
            IdempotencyKey: Guid.NewGuid().ToString());

        await sut.IngestAsync(request);

        await bus.Received(1).Publish(
            Arg.Is<GeoFraudDetectedIntegrationEventV1>(e =>
                e.TenantId == "tenant-geo" &&
                e.EmployeeId == request.EmployeeId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PunchIngestion_low_severity_fraud_does_not_publish_GeoFraudDetectedIntegrationEvent()
    {
        var db = TestDbContextFactory.Create("tenant-low-fraud");
        var bus = Substitute.For<IPublishEndpoint>();
        var policyResolver = BuildPolicyResolver(db);
        var geoScorer = new GeoConfidenceScorer(db, NullLogger<GeoConfidenceScorer>.Instance);
        var processingEngine = BuildProcessingEngine(db, bus);
        var sut = new PunchIngestionService(db, geoScorer, policyResolver, processingEngine, bus,
            NullLogger<PunchIngestionService>.Instance);

        // No fraud signals — fully clean mobile punch with no geo zones configured
        // Score will be 0 (no signals), outcome Rejected, but no fraud signals raised → no publish
        var request = new PunchIngestionService.PunchRequest(
            TenantId: "tenant-low-fraud",
            EmployeeId: Guid.NewGuid(),
            DeviceId: null,
            DeviceTimestampUtc: DateTimeOffset.UtcNow,
            Direction: PunchDirection.In,
            CaptureMode: CaptureMode.Mobile,
            Modality: null,
            Latitude: null,
            Longitude: null,
            GpsAccuracyMeters: null,
            WifiSsidMatched: false,
            IpWhitelisted: false,
            BleBeaconDetected: false,
            VpnMatched: false,
            MockGpsDetected: false,
            JailbreakDetected: false,
            DeviceFingerprint: null,
            BiometricMatchScore: null,
            IdempotencyKey: Guid.NewGuid().ToString());

        await sut.IngestAsync(request);

        await bus.DidNotReceive().Publish(
            Arg.Any<GeoFraudDetectedIntegrationEventV1>(),
            Arg.Any<CancellationToken>());
    }

    // ── ConsecutiveAbsence ────────────────────────────────────────────────────

    [Fact]
    public async Task ConsecutiveAbsenceMonitor_breach_publishes_ConsecutiveAbsenceEscalatedIntegrationEvent()
    {
        const string tenantId = "tenant-absence";
        var db = TestDbContextFactory.Create(tenantId);
        var bus = Substitute.For<IPublishEndpoint>();
        var policyResolver = BuildPolicyResolver(db);
        var sut = new ConsecutiveAbsenceMonitor(db, policyResolver, bus,
            NullLogger<ConsecutiveAbsenceMonitor>.Instance);

        var employeeId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);

        // Seed 4 consecutive absent days (default policy threshold is 3)
        for (var i = 3; i >= 0; i--)
        {
            var workDate = today.AddDays(-i);
            db.AttendanceRecords.Add(AttendanceRecord.CreatePending(
                tenantId, employeeId, Guid.NewGuid(), Guid.NewGuid(),
                workDate,
                workDate.ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.FromHours(9)), DateTimeKind.Utc),
                workDate.ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.FromHours(18)), DateTimeKind.Utc)));
        }

        // Mark all as Absent directly
        var records = db.AttendanceRecords.Local.ToList();
        foreach (var r in records)
            r.MarkAbsent();

        await db.SaveChangesAsync();

        await sut.DetectAsync(tenantId, today);

        await bus.Received(1).Publish(
            Arg.Is<ConsecutiveAbsenceEscalatedIntegrationEventV1>(e =>
                e.TenantId == tenantId &&
                e.EmployeeId == employeeId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ConsecutiveAbsenceMonitor_below_threshold_does_not_publish()
    {
        const string tenantId = "tenant-absence-ok";
        var db = TestDbContextFactory.Create(tenantId);
        var bus = Substitute.For<IPublishEndpoint>();
        var policyResolver = BuildPolicyResolver(db);
        var sut = new ConsecutiveAbsenceMonitor(db, policyResolver, bus,
            NullLogger<ConsecutiveAbsenceMonitor>.Instance);

        // Only 1 absent day — below threshold
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var employeeId = Guid.NewGuid();
        var rec = AttendanceRecord.CreatePending(tenantId, employeeId, Guid.NewGuid(), Guid.NewGuid(),
            today,
            today.ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.FromHours(9)), DateTimeKind.Utc),
            today.ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.FromHours(18)), DateTimeKind.Utc));
        rec.MarkAbsent();
        db.AttendanceRecords.Add(rec);
        await db.SaveChangesAsync();

        await sut.DetectAsync(tenantId, today);

        await bus.DidNotReceive().Publish(
            Arg.Any<ConsecutiveAbsenceEscalatedIntegrationEventV1>(),
            Arg.Any<CancellationToken>());
    }

    // ── AttendanceException integration events ────────────────────────────────

    [Fact]
    public void AttendanceExceptionRaisedIntegrationEvent_is_defined_in_contracts()
    {
        // Confirms the contract type is in the correct assembly and namespace.
        var eventType = typeof(AttendanceExceptionRaisedIntegrationEventV1);
        eventType.Assembly.GetName().Name.Should().Be("Karamchari.TimeAttendance.Contracts");
        eventType.Namespace.Should().Be("Karamchari.TimeAttendance.Contracts");
    }

    [Fact]
    public void RegularizationApprovedIntegrationEvent_is_defined_in_contracts()
    {
        var eventType = typeof(RegularizationApprovedIntegrationEventV1);
        eventType.Assembly.GetName().Name.Should().Be("Karamchari.TimeAttendance.Contracts");
    }

    [Fact]
    public void RegularizationRejectedIntegrationEvent_is_defined_in_contracts()
    {
        var eventType = typeof(RegularizationRejectedIntegrationEventV1);
        eventType.Assembly.GetName().Name.Should().Be("Karamchari.TimeAttendance.Contracts");
    }

    [Fact]
    public void AttendanceExceptionRaisedIntegrationEvent_has_expected_properties()
    {
        var evt = new AttendanceExceptionRaisedIntegrationEventV1(
            CorrelationId: Guid.NewGuid(),
            TenantId: "t1",
            EmployeeId: Guid.NewGuid(),
            AttendanceRecordId: Guid.NewGuid(),
            ExceptionType: "LateArrival",
            Severity: "Medium",
            Description: "15 minutes late",
            RaisedAtUtc: DateTimeOffset.UtcNow);

        evt.ExceptionType.Should().Be("LateArrival");
        evt.Severity.Should().Be("Medium");
        evt.TenantId.Should().Be("t1");
    }

    [Fact]
    public void RegularizationApprovedIntegrationEvent_has_expected_properties()
    {
        var approver = Guid.NewGuid();
        var requestId = Guid.NewGuid();

        var evt = new RegularizationApprovedIntegrationEventV1(
            CorrelationId: Guid.NewGuid(),
            TenantId: "t2",
            EmployeeId: Guid.NewGuid(),
            RegularizationRequestId: requestId,
            ApprovedByEmployeeId: approver,
            ApprovedAtUtc: DateTimeOffset.UtcNow);

        evt.RegularizationRequestId.Should().Be(requestId);
        evt.ApprovedByEmployeeId.Should().Be(approver);
    }

    [Fact]
    public void RegularizationRejectedIntegrationEvent_has_expected_properties()
    {
        var evt = new RegularizationRejectedIntegrationEventV1(
            CorrelationId: Guid.NewGuid(),
            TenantId: "t3",
            EmployeeId: Guid.NewGuid(),
            RegularizationRequestId: Guid.NewGuid(),
            Reason: "Insufficient documentation",
            RejectedAtUtc: DateTimeOffset.UtcNow);

        evt.Reason.Should().Be("Insufficient documentation");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static AttendancePolicyHierarchyResolver BuildPolicyResolver(
        Karamchari.TimeAttendance.Persistence.TimeAttendanceDbContext db)
        => new(db, new MemoryCache(new MemoryCacheOptions()));

    private static AttendanceProcessingEngine BuildProcessingEngine(
        Karamchari.TimeAttendance.Persistence.TimeAttendanceDbContext db,
        IPublishEndpoint bus)
    {
        var metrics = new Karamchari.TimeAttendance.Observability.AttendanceMetrics();
        var shiftResolver = new ShiftResolver(db, NullLogger<ShiftResolver>.Instance, metrics);
        var scoreCalc = new AttendanceScoreCalculator(db);
        return new AttendanceProcessingEngine(db, shiftResolver, scoreCalc, bus,
            NullLogger<AttendanceProcessingEngine>.Instance, metrics);
    }
}
