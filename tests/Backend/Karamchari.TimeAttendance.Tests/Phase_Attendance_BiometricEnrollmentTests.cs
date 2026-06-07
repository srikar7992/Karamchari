using FluentAssertions;
using Karamchari.TimeAttendance.Domain.Biometric;
using Xunit;

namespace Karamchari.TimeAttendance.Tests;

public sealed class BiometricEnrollmentSessionTests
{
    private const string TenantId = "tenant-enroll-1";

    [Fact]
    public void Start_creates_session_in_progress()
    {
        var session = BiometricEnrollmentSession.Start(
            TenantId, Guid.NewGuid(), BiometricModality.Fingerprint, "operator-1");

        session.Status.Should().Be(EnrollmentSessionStatus.InProgress);
        session.SamplesCaptured.Should().Be(0);
        session.ConsentObtained.Should().BeFalse();
    }

    [Fact]
    public void RecordSample_below_quality_threshold_sets_failed_status()
    {
        var session = BiometricEnrollmentSession.Start(
            TenantId, Guid.NewGuid(), BiometricModality.Face, "op",
            minimumQualityThreshold: 70);

        var ready = session.RecordSample(40); // below 70

        ready.Should().BeFalse();
        session.Status.Should().Be(EnrollmentSessionStatus.QualityCheckFailed);
    }

    [Fact]
    public void RecordSample_returns_true_when_required_samples_met()
    {
        var session = BiometricEnrollmentSession.Start(
            TenantId, Guid.NewGuid(), BiometricModality.Fingerprint, "op",
            requiredSampleCount: 3, minimumQualityThreshold: 60);

        session.RecordSample(80).Should().BeFalse();
        session.RecordSample(85).Should().BeFalse();
        var done = session.RecordSample(90);

        done.Should().BeTrue();
        session.SamplesCaptured.Should().Be(3);
    }

    [Fact]
    public void Complete_without_consent_throws()
    {
        var session = BiometricEnrollmentSession.Start(
            TenantId, Guid.NewGuid(), BiometricModality.Iris, "op",
            requiredSampleCount: 1);

        session.RecordSample(80);
        // consent NOT given

        var act = () => session.Complete(Guid.NewGuid());
        act.Should().Throw<InvalidOperationException>().WithMessage("*consent*");
    }

    [Fact]
    public void Complete_with_insufficient_samples_throws()
    {
        var session = BiometricEnrollmentSession.Start(
            TenantId, Guid.NewGuid(), BiometricModality.PalmVein, "op",
            requiredSampleCount: 3);

        session.RecordConsent();
        session.RecordSample(80); // only 1 of 3

        var act = () => session.Complete(Guid.NewGuid());
        act.Should().Throw<InvalidOperationException>().WithMessage("*Insufficient*");
    }

    [Fact]
    public void Happy_path_enrollment_completes_successfully()
    {
        var session = BiometricEnrollmentSession.Start(
            TenantId, Guid.NewGuid(), BiometricModality.Face, "op",
            requiredSampleCount: 2);

        session.RecordConsent();
        session.RecordSample(82);
        session.RecordSample(88);

        var templateId = Guid.NewGuid();
        session.Complete(templateId);

        session.Status.Should().Be(EnrollmentSessionStatus.Completed);
        session.ResultTemplateId.Should().Be(templateId);
        session.CompletedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void RetryAfterQualityFailure_resets_sample_count()
    {
        var session = BiometricEnrollmentSession.Start(
            TenantId, Guid.NewGuid(), BiometricModality.Fingerprint, "op",
            minimumQualityThreshold: 75);

        session.RecordSample(30); // fails
        session.Status.Should().Be(EnrollmentSessionStatus.QualityCheckFailed);

        session.RetryAfterQualityFailure();

        session.Status.Should().Be(EnrollmentSessionStatus.InProgress);
        session.SamplesCaptured.Should().Be(0);
    }

    [Fact]
    public void Cancel_sets_cancelled_status()
    {
        var session = BiometricEnrollmentSession.Start(
            TenantId, Guid.NewGuid(), BiometricModality.SmartCard, "op");

        session.Cancel();

        session.Status.Should().Be(EnrollmentSessionStatus.Cancelled);
    }
}
