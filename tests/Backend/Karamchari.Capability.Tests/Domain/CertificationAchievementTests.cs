// -----------------------------------------------------------------------
// <copyright file="CertificationAchievementTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Karamchari.Capability.Domain.Learning;
using Karamchari.Capability.Domain.Primitives;
using Xunit;

namespace Karamchari.Capability.Tests.Domain;

public class CertificationAchievementTests
{
    private static CertificationAchievement RecordCert(DateTimeOffset? expiresAt = null)
        => CertificationAchievement.Record(
            "tenant-1",
            Guid.NewGuid(),
            "AWS Solutions Architect",
            "Amazon",
            "AWS-SA-12345",
            DateTimeOffset.UtcNow.AddDays(-60),
            expiresAt);

    [Fact]
    public void Certification_Record_StatusIsActive()
    {
        // No expiry → always active
        var cert = RecordCert(expiresAt: null);

        cert.Status.Should().Be(CertificationStatus.Active);
    }

    [Fact]
    public void Certification_Record_FutureExpiry_StatusIsActive()
    {
        var cert = RecordCert(expiresAt: DateTimeOffset.UtcNow.AddDays(60));

        cert.Status.Should().Be(CertificationStatus.Active);
    }

    [Fact]
    public void Certification_EvaluateStatus_WhenExpired_StatusIsExpired()
    {
        var cert = RecordCert(expiresAt: DateTimeOffset.UtcNow.AddDays(-1));

        cert.EvaluateStatus();

        cert.Status.Should().Be(CertificationStatus.Expired);
    }

    [Fact]
    public void Certification_EvaluateStatus_Within30DaysOfExpiry_StatusIsExpiringSoon()
    {
        // Expires in 15 days — within the 30-day warning window
        var cert = RecordCert(expiresAt: DateTimeOffset.UtcNow.AddDays(15));

        cert.EvaluateStatus();

        cert.Status.Should().Be(CertificationStatus.ExpiringSoon);
    }

    [Fact]
    public void Certification_EvaluateStatus_NotNearExpiry_StatusRemainsActive()
    {
        var cert = RecordCert(expiresAt: DateTimeOffset.UtcNow.AddDays(90));

        cert.EvaluateStatus();

        cert.Status.Should().Be(CertificationStatus.Active);
    }

    [Fact]
    public void Certification_Revoke_SetsStatusToRevoked()
    {
        var cert = RecordCert(expiresAt: DateTimeOffset.UtcNow.AddDays(60));

        cert.Revoke("Policy violation");

        cert.Status.Should().Be(CertificationStatus.Revoked);
    }

    [Fact]
    public void Certification_EvaluateStatus_WhenRevoked_StatusRemainsRevoked()
    {
        var cert = RecordCert(expiresAt: DateTimeOffset.UtcNow.AddDays(15));
        cert.Revoke("Policy violation");

        // EvaluateStatus should NOT override Revoked
        cert.EvaluateStatus();

        cert.Status.Should().Be(CertificationStatus.Revoked);
    }

    [Fact]
    public void Certification_Record_SetsProperties()
    {
        var employeeId = Guid.NewGuid();
        var issued = DateTimeOffset.UtcNow.AddDays(-30);
        var expires = DateTimeOffset.UtcNow.AddDays(335);

        var cert = CertificationAchievement.Record(
            "tenant-1", employeeId, "Azure Fundamentals", "Microsoft", "AZ-900-XYZ", issued, expires);

        cert.TenantId.Should().Be("tenant-1");
        cert.EmployeeId.Should().Be(employeeId);
        cert.Name.Should().Be("Azure Fundamentals");
        cert.Authority.Should().Be("Microsoft");
        cert.LicenseNumber.Should().Be("AZ-900-XYZ");
        cert.IssuedAtUtc.Should().Be(issued);
        cert.ExpiresAtUtc.Should().Be(expires);
        cert.Id.Should().NotBeEmpty();
    }
}
