using FluentAssertions;
using Karamchari.Core.Security.Tenant;
using Karamchari.TenantIsolationCertification.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Karamchari.TenantIsolationCertification.Security;

public sealed class TenantAttackSimulatorTests
{
    private readonly TenantAttackSimulator _simulator;

    public TenantAttackSimulatorTests()
    {
        _simulator = new TenantAttackSimulator(NullLogger<TenantAttackSimulator>.Instance);
    }

    [Fact]
    public async Task SimulateTenantEnumeration_ShouldTrackAttempts()
    {
        var tenantId = "acme";

        await _simulator.SimulateTenantEnumerationAsync(tenantId);

        var report = _simulator.GetAttackReport(tenantId);
        report.TotalAttempts.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task SimulateReplayInjection_ShouldTrackInjection()
    {
        var sourceTenant = "acme";
        var targetTenant = "globex";
        var messageIds = new List<string> { Guid.NewGuid().ToString() };

        await _simulator.SimulateReplayInjectionAsync(sourceTenant, targetTenant, messageIds);

        var report = _simulator.GetAttackReport(sourceTenant);
        report.TotalAttempts.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task SimulateStaleTokenValidation_ShouldTrackAttempts()
    {
        var tenantId = "acme";
        var tokenAge = TimeSpan.FromHours(2);

        await _simulator.SimulateStaleTokenValidationAsync(tenantId, tokenAge);

        var report = _simulator.GetAttackReport(tenantId);
        report.TotalAttempts.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task SimulateImpersonationAttempt_ShouldTrackAttempts()
    {
        var impersonator = "acme";
        var targetUser = "user123";
        var targetTenant = "globex";

        await _simulator.SimulateImpersonationAttemptAsync(impersonator, targetUser, targetTenant);

        var report = _simulator.GetAttackReport(impersonator);
        report.TotalAttempts.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task SimulateForgedHeaderInjection_ShouldTrackInjection()
    {
        var tenantId = "acme";
        var forgedTenantId = "globex";

        await _simulator.SimulateForgedHeaderInjectionAsync(tenantId, forgedTenantId);

        var report = _simulator.GetAttackReport(tenantId);
        report.TotalAttempts.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ClearAttackAttempts_ShouldClearAll()
    {
        _simulator.SimulateTenantEnumerationAsync("acme").Wait();

        _simulator.ClearAttackAttempts();

        var report = _simulator.GetAttackReport("acme");
        report.TotalAttempts.Should().Be(0);
    }
}

public sealed class ReplayAttackValidatorTests
{
    private readonly ReplayAttackValidator _validator;

    public ReplayAttackValidatorTests()
    {
        _validator = new ReplayAttackValidator(NullLogger<ReplayAttackValidator>.Instance);
    }

    [Fact]
    public void ValidateReplayProtection_ShouldRejectDuplicates()
    {
        var messageId = Guid.NewGuid().ToString();

        var firstResult = _validator.ValidateReplayProtection(messageId);
        var secondResult = _validator.ValidateReplayProtection(messageId);

        firstResult.Should().BeTrue();
        secondResult.Should().BeFalse();
    }

    [Fact]
    public void ValidateReplayProtection_ShouldRejectStaleMessages()
    {
        var messageId = Guid.NewGuid().ToString();
        var oldTimestamp = DateTime.UtcNow - TimeSpan.FromHours(2);

        var result = _validator.ValidateReplayProtection(messageId, null, oldTimestamp);

        result.Should().BeFalse();
    }

    [Fact]
    public void HandleDuplicateReplay_ShouldRejectDuplicates()
    {
        var messageId = Guid.NewGuid().ToString();

        var firstResult = _validator.HandleDuplicateReplay(messageId);
        var secondResult = _validator.HandleDuplicateReplay(messageId);

        firstResult.Should().BeTrue();
        secondResult.Should().BeFalse();
    }

    [Fact]
    public void DetectStaleReplay_ShouldDetectStale()
    {
        var messageId = Guid.NewGuid().ToString();
        var oldTimestamp = DateTime.UtcNow - TimeSpan.FromHours(2);

        // Record the message with an old timestamp
        _validator.RecordStaleMessage(messageId, oldTimestamp);

        var result = _validator.DetectStaleReplay(messageId, TimeSpan.FromHours(1));

        result.Should().BeTrue();
    }

    [Fact]
    public void GetReport_ShouldReturnCorrectStats()
    {
        _validator.ValidateReplayProtection(Guid.NewGuid().ToString());

        // When ValidateReplayProtection is called with a stale timestamp, it returns false
        // and records it as a stale message, but it DOES NOT add it to _processedMessages.
        // Therefore, TotalMessagesProcessed is 1, not 2.
        _validator.ValidateReplayProtection(Guid.NewGuid().ToString(), null, DateTime.UtcNow - TimeSpan.FromHours(2));

        var report = _validator.GetReport();

        report.TotalMessagesProcessed.Should().Be(1);
        report.StaleReplaysDetected.Should().Be(1);
    }

    [Fact]
    public void Clear_ShouldResetState()
    {
        _validator.ValidateReplayProtection(Guid.NewGuid().ToString());

        _validator.Clear();

        var report = _validator.GetReport();
        report.TotalMessagesProcessed.Should().Be(0);
    }
}

public sealed class ForgedHeaderValidatorTests
{
    private readonly ForgedHeaderValidator _validator;

    public ForgedHeaderValidatorTests()
    {
        _validator = new ForgedHeaderValidator(NullLogger<ForgedHeaderValidator>.Instance);
    }

    [Fact]
    public void ValidateHeader_ShouldRejectMissingHeader()
    {
        var result = _validator.ValidateHeader(null, "value", "tenant");

        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateHeader_ShouldAcceptValidHeader()
    {
        var result = _validator.ValidateHeader("X-Tenant-Id", "acme", "acme");

        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateHeaderTampering_ShouldDetectTampering()
    {
        var original = "acme";
        var tampered = "globex";

        var result = _validator.ValidateHeaderTampering(original, tampered);

        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateMissingHeader_ShouldRejectWhenNotAllowed()
    {
        var result = _validator.ValidateMissingHeader(null, allowMissing: false);

        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateMissingHeader_ShouldAllowWhenConfigured()
    {
        var result = _validator.ValidateMissingHeader(null, allowMissing: true);

        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateTenantIdFormat_ShouldRejectInvalidFormats()
    {
        var invalidIds = new[] { "", new string('a', 51), "test;drop table", "test\x00test" };

        foreach (var id in invalidIds)
        {
            _validator.ValidateTenantIdFormat(id).Should().BeFalse(id);
        }
    }

    [Fact]
    public void ValidateTenantIdFormat_ShouldAcceptValidFormats()
    {
        var validIds = new[] { "acme", "acme-corp", "acme_123", "a1b2c3" };

        foreach (var id in validIds)
        {
            _validator.ValidateTenantIdFormat(id).Should().BeTrue(id);
        }
    }

    [Fact]
    public void ValidateCrossTenantHeader_ShouldRejectCrossTenant()
    {
        var result = _validator.ValidateCrossTenantHeader("acme", "globex");

        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateCrossTenantHeader_ShouldAcceptSameTenant()
    {
        var result = _validator.ValidateCrossTenantHeader("acme", "acme");

        result.Should().BeTrue();
    }
}

public sealed class StaleTokenValidatorTests
{
    private readonly StaleTokenValidator _validator;

    public StaleTokenValidatorTests()
    {
        _validator = new StaleTokenValidator(
            NullLogger<StaleTokenValidator>.Instance,
            TimeSpan.FromHours(1));
    }

    [Fact]
    public void ValidateTokenExpiration_ShouldRejectExpiredToken()
    {
        var tokenIssuedAt = DateTime.UtcNow - TimeSpan.FromHours(2);

        var result = _validator.ValidateTokenExpiration("token1", tokenIssuedAt, DateTime.UtcNow);

        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateTokenExpiration_ShouldAcceptValidToken()
    {
        var tokenIssuedAt = DateTime.UtcNow - TimeSpan.FromMinutes(30);

        var result = _validator.ValidateTokenExpiration("token1", tokenIssuedAt, DateTime.UtcNow);

        result.Should().BeTrue();
    }

    [Fact]
    public void RejectStaleToken_ShouldRejectExpiredToken()
    {
        var tokenIssuedAt = DateTime.UtcNow - TimeSpan.FromHours(2);

        var result = _validator.RejectStaleToken("token1", tokenIssuedAt);

        result.Should().BeFalse();
    }

    [Fact]
    public void PreventTokenReuse_ShouldRejectReusedToken()
    {
        var tokenId = "token1";

        _validator.PreventTokenReuse(tokenId);
        var result = _validator.PreventTokenReuse(tokenId);

        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateTokenNotRevoked_ShouldRejectRevoked()
    {
        var tokenId = "token1";
        var revokedTokens = new List<string> { "token1" };

        var result = _validator.ValidateTokenNotRevoked(tokenId, revokedTokens);

        result.Should().BeFalse();
    }

    [Fact]
    public void GetTokenExpirationWindow_ShouldReturnConfigured()
    {
        var window = _validator.GetTokenExpirationWindow();

        window.Should().Be(TimeSpan.FromHours(1));
    }
}

public sealed class ImpersonationValidatorTests
{
    private readonly ImpersonationValidator _validator;

    public ImpersonationValidatorTests()
    {
        _validator = new ImpersonationValidator(NullLogger<ImpersonationValidator>.Instance);
    }

    [Fact]
    public void ValidateCrossTenantImpersonation_ShouldBlockCrossTenant()
    {
        var result = _validator.ValidateCrossTenantImpersonation(
            "acme",
            "globex",
            "user123");

        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateCrossTenantImpersonation_ShouldAllowSameTenant()
    {
        var result = _validator.ValidateCrossTenantImpersonation(
            "acme",
            "acme",
            "user123");

        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateTenantSpecificImpersonation_ShouldAllowValidUser()
    {
        IReadOnlyDictionary<string, IReadOnlyList<string>> tenantUsers = new Dictionary<string, IReadOnlyList<string>>
        {
            { "acme", new List<string> { "user1", "user2" } }
        };

        var result = _validator.ValidateTenantSpecificImpersonation(
            "acme",
            "user1",
            tenantUsers);

        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateTenantSpecificImpersonation_ShouldBlockInvalidUser()
    {
        IReadOnlyDictionary<string, IReadOnlyList<string>> tenantUsers = new Dictionary<string, IReadOnlyList<string>>
        {
            { "acme", new List<string> { "user1", "user2" } }
        };

        var result = _validator.ValidateTenantSpecificImpersonation(
            "acme",
            "user999",
            tenantUsers);

        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateAdminImpersonation_ShouldBlockCrossTenantAdmin()
    {
        var result = _validator.ValidateAdminImpersonation(
            "acme",
            "globex",
            "admin",
            canImpersonateAnyTenant: false);

        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateAdminImpersonation_ShouldAllowAnyTenantWhenPermitted()
    {
        var result = _validator.ValidateAdminImpersonation(
            "acme",
            "globex",
            "admin",
            canImpersonateAnyTenant: true);

        result.Should().BeTrue();
    }

    [Fact]
    public void GetReport_ShouldReturnCorrectStats()
    {
        _validator.ValidateCrossTenantImpersonation("acme", "globex", "user1");
        _validator.ValidateCrossTenantImpersonation("acme", "acme", "user2");
        _validator.ValidateCrossTenantImpersonation("globex", "globex", "user3");

        var report = _validator.GetReport();

        report.TotalAttempts.Should().Be(3);
        report.BlockedAttempts.Should().Be(1);
    }

    [Fact]
    public void Clear_ShouldResetState()
    {
        _validator.ValidateCrossTenantImpersonation("acme", "globex", "user1");

        _validator.Clear();

        var report = _validator.GetReport();
        report.TotalAttempts.Should().Be(0);
    }
}
