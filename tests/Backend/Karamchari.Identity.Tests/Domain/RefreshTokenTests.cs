// -----------------------------------------------------------------------
// <copyright file="RefreshTokenTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Karamchari.Identity.Domain;
using Xunit;

namespace Karamchari.Identity.Tests.Domain;

public sealed class RefreshTokenTests
{
    private static RefreshToken CreateActive(TimeSpan? lifetime = null) =>
        RefreshToken.Create(
            tokenHash: "hash-abc",
            userId: Guid.NewGuid(),
            tenantId: "tenant-1",
            expiresAt: DateTimeOffset.UtcNow.Add(lifetime ?? TimeSpan.FromDays(7)));

    // ── Create ───────────────────────────────────────────────────────────────

    [Fact]
    public void RefreshToken_Create_SetsProperties()
    {
        var userId = Guid.NewGuid();
        var expiresAt = DateTimeOffset.UtcNow.AddDays(7);

        var token = RefreshToken.Create("myhash", userId, "t-1", expiresAt, "iPhone");

        token.TokenHash.Should().Be("myhash");
        token.UserId.Should().Be(userId);
        token.TenantId.Should().Be("t-1");
        token.DeviceInfo.Should().Be("iPhone");
        token.ExpiresAtUtc.Should().BeCloseTo(expiresAt, TimeSpan.FromSeconds(1));
        token.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void RefreshToken_Create_IsActive()
    {
        var token = CreateActive();

        token.IsActive.Should().BeTrue();
        token.IsRevoked.Should().BeFalse();
        token.IsExpired.Should().BeFalse();
    }

    // ── Revoke ───────────────────────────────────────────────────────────────

    [Fact]
    public void RefreshToken_Revoke_SetsRevokedAt()
    {
        var token = CreateActive();

        token.Revoke("192.168.1.1");

        token.IsRevoked.Should().BeTrue();
        token.IsActive.Should().BeFalse();
        token.RevokedAtUtc.Should().NotBeNull();
        token.RevokedByIp.Should().Be("192.168.1.1");
    }

    [Fact]
    public void RefreshToken_Revoke_WithReplacement_SetsReplacedByHash()
    {
        var token = CreateActive();

        token.Revoke("10.0.0.1", "new-hash-xyz");

        token.ReplacedByTokenHash.Should().Be("new-hash-xyz");
    }

    // ── Expired ──────────────────────────────────────────────────────────────

    [Fact]
    public void RefreshToken_Expired_IsNotActive()
    {
        var token = RefreshToken.Create("hash", Guid.NewGuid(), "t-1",
            DateTimeOffset.UtcNow.AddSeconds(-1));

        token.IsExpired.Should().BeTrue();
        token.IsActive.Should().BeFalse();
    }

    // ── RevokedToken ─────────────────────────────────────────────────────────

    [Fact]
    public void RevokedToken_Create_SetsJtiAndExpiry()
    {
        var jti = Guid.NewGuid().ToString();
        var expiresAt = DateTimeOffset.UtcNow.AddHours(1);

        var revoked = RevokedToken.Create(jti, expiresAt);

        revoked.Jti.Should().Be(jti);
        revoked.ExpiresAtUtc.Should().BeCloseTo(expiresAt, TimeSpan.FromSeconds(1));
        revoked.RevokedAtUtc.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }
}
