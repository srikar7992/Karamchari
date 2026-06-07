using FluentAssertions;
using Karamchari.TimeAttendance.Domain.Biometric;
using Xunit;

namespace Karamchari.TimeAttendance.Tests;

public sealed class BiometricTemplateTests
{
    private const string TenantId = "tenant-bio-1";

    [Fact]
    public void Enroll_creates_active_template_with_correct_properties()
    {
        var template = BiometricTemplate.Enroll(
            TenantId,
            Guid.NewGuid(),
            BiometricModality.Fingerprint,
            "base64-encrypted-blob",
            "kms-key-v1",
            qualityScore: 85,
            BiometricMatchMode.OnServer);

        template.IsActive.Should().BeTrue();
        template.QualityScore.Should().Be(85);
        template.Modality.Should().Be(BiometricModality.Fingerprint);
        template.EncryptedTemplateBlob.Should().Be("base64-encrypted-blob");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Enroll_rejects_out_of_range_quality_score(int badScore)
    {
        var act = () => BiometricTemplate.Enroll(
            TenantId, Guid.NewGuid(), BiometricModality.Face,
            "blob", "kms", qualityScore: badScore, BiometricMatchMode.OnDevice);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void AuthorizeDevice_adds_device_id()
    {
        var template = BiometricTemplate.Enroll(
            TenantId, Guid.NewGuid(), BiometricModality.Face,
            "blob", "kms-v1", 90, BiometricMatchMode.OnDevice);

        var deviceId = Guid.NewGuid();
        template.AuthorizeDevice(deviceId);

        template.AuthorizedDeviceIds.Should().Contain(deviceId);
    }

    [Fact]
    public void AuthorizeDevice_is_idempotent()
    {
        var template = BiometricTemplate.Enroll(
            TenantId, Guid.NewGuid(), BiometricModality.Iris,
            "blob", "kms-v1", 78, BiometricMatchMode.OnServer);

        var deviceId = Guid.NewGuid();
        template.AuthorizeDevice(deviceId);
        template.AuthorizeDevice(deviceId); // second call is no-op

        template.AuthorizedDeviceIds.Should().HaveCount(1);
    }

    [Fact]
    public void Revoke_clears_blob_and_marks_inactive()
    {
        var template = BiometricTemplate.Enroll(
            TenantId, Guid.NewGuid(), BiometricModality.PalmVein,
            "sensitive-blob", "kms-v1", 80, BiometricMatchMode.OnServer);

        template.Revoke();

        template.IsActive.Should().BeFalse();
        template.EncryptedTemplateBlob.Should().BeEmpty();
        template.AuthorizedDeviceIds.Should().BeEmpty();
    }

    [Fact]
    public void Revoke_twice_throws()
    {
        var template = BiometricTemplate.Enroll(
            TenantId, Guid.NewGuid(), BiometricModality.SmartCard,
            "blob", "kms-v1", 70, BiometricMatchMode.OnDevice);

        template.Revoke();
        var act = () => template.Revoke();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void RotateKey_updates_blob_and_version()
    {
        var template = BiometricTemplate.Enroll(
            TenantId, Guid.NewGuid(), BiometricModality.Fingerprint,
            "old-blob", "kms-v1", 92, BiometricMatchMode.OnServer);

        template.RotateKey("new-blob", "kms-v2");

        template.EncryptedTemplateBlob.Should().Be("new-blob");
        template.KmsKeyVersion.Should().Be("kms-v2");
    }

    [Fact]
    public void AuthorizeDevice_on_revoked_template_throws()
    {
        var template = BiometricTemplate.Enroll(
            TenantId, Guid.NewGuid(), BiometricModality.Iris,
            "blob", "kms-v1", 88, BiometricMatchMode.OnServer);

        template.Revoke();
        var act = () => template.AuthorizeDevice(Guid.NewGuid());

        act.Should().Throw<InvalidOperationException>();
    }
}
