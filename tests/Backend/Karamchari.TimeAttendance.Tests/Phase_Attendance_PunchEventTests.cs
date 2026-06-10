// -----------------------------------------------------------------------
// <copyright file="Phase_Attendance_PunchEventTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Karamchari.TimeAttendance.Domain.Biometric;
using Xunit;

namespace Karamchari.TimeAttendance.Tests;

public sealed class PunchEventTests
{
    private const string TenantId = "tenant-punch-1";

    private static PunchEvent CreateValidPunch(
        PunchDirection direction = PunchDirection.In,
        double? lat = 12.97,
        double? lng = 77.59,
        int geoScore = 85) =>
        PunchEvent.Record(
            TenantId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            direction,
            CaptureMode.Biometric,
            BiometricModality.Fingerprint,
            lat, lng,
            gpsAccuracyMeters: 10,
            geoConfidenceScore: geoScore,
            biometricMatchScore: 0.97,
            idempotencyKey: Guid.NewGuid().ToString());

    [Fact]
    public void Record_creates_accepted_punch_event()
    {
        var punch = CreateValidPunch();

        punch.IsAccepted.Should().BeTrue();
        punch.RejectionReason.Should().BeNull();
        punch.TenantId.Should().Be(TenantId);
    }

    [Fact]
    public void Record_clamps_geo_score_to_100()
    {
        var punch = PunchEvent.Record(TenantId, Guid.NewGuid(), null,
            DateTimeOffset.UtcNow, PunchDirection.Out, CaptureMode.WebPortal,
            null, null, null, null, geoConfidenceScore: 150, null, "key");

        punch.GeoConfidenceScore.Should().Be(100);
    }

    [Fact]
    public void Record_clamps_negative_geo_score_to_zero()
    {
        var punch = PunchEvent.Record(TenantId, Guid.NewGuid(), null,
            DateTimeOffset.UtcNow, PunchDirection.In, CaptureMode.Mobile,
            null, null, null, null, geoConfidenceScore: -10, null, "key2");

        punch.GeoConfidenceScore.Should().Be(0);
    }

    [Fact]
    public void Reject_sets_rejection_reason_and_marks_not_accepted()
    {
        var punch = CreateValidPunch();
        punch.Reject("Impossible travel detected.");

        punch.IsAccepted.Should().BeFalse();
        punch.RejectionReason.Should().Be("Impossible travel detected.");
    }

    [Fact]
    public void Reject_with_empty_reason_throws()
    {
        var punch = CreateValidPunch();
        var act = () => punch.Reject(string.Empty);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Record_with_empty_idempotency_key_throws()
    {
        var act = () => PunchEvent.Record(
            TenantId, Guid.NewGuid(), null,
            DateTimeOffset.UtcNow, PunchDirection.In, CaptureMode.Mobile,
            null, null, null, null, 70, null, idempotencyKey: "");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AuthoritativeTimestamp_equals_received_at_utc()
    {
        var before = DateTimeOffset.UtcNow;
        var punch = CreateValidPunch();
        var after = DateTimeOffset.UtcNow;

        punch.AuthoritativeTimestamp.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void ClockSkewFlagged_round_trips_correctly()
    {
        var punch = PunchEvent.Record(
            TenantId, Guid.NewGuid(), null,
            DateTimeOffset.UtcNow.AddMinutes(-10),
            PunchDirection.In, CaptureMode.Biometric,
            BiometricModality.Face, 12.0, 77.0, 5, 80, 0.95,
            Guid.NewGuid().ToString(),
            clockSkewFlagged: true);

        punch.ClockSkewFlagged.Should().BeTrue();
    }
}
