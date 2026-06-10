// -----------------------------------------------------------------------
// <copyright file="ChaosGeneratorsTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Karamchari.Core.Chaos.Tenant;
using Karamchari.TenantIsolationCertification.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Karamchari.TenantIsolationCertification.Chaos;

public sealed class TenantChaosEngineTests
{
    private readonly TenantChaosEngine _engine;

    public TenantChaosEngineTests()
    {
        _engine = new TenantChaosEngine(NullLogger<TenantChaosEngine>.Instance);
    }

    [Fact]
    public async Task InjectDbOutage_ShouldEnableDbOutage()
    {
        var tenantId = "acme";

        await _engine.InjectDbOutageAsync(tenantId);

        _engine.IsTenantAffected(tenantId, "DbOutage").Should().BeTrue();
    }

    [Fact]
    public async Task InjectRedisOutage_ShouldEnableRedisOutage()
    {
        var tenantId = "globex";

        await _engine.InjectRedisOutageAsync(tenantId);

        _engine.IsTenantAffected(tenantId, "RedisOutage").Should().BeTrue();
    }

    [Fact]
    public async Task InjectBrokerDelay_ShouldSetDelay()
    {
        var tenantId = "initech";
        var delay = TimeSpan.FromSeconds(30);

        await _engine.InjectBrokerDelayAsync(tenantId, delay);

        _engine.IsTenantAffected(tenantId, "BrokerDelay").Should().BeTrue();
    }

    [Fact]
    public async Task InjectPacketLoss_ShouldSetPercentage()
    {
        var tenantId = "acme";
        var percentage = 25.0;

        await _engine.InjectPacketLossAsync(tenantId, percentage);

        _engine.IsTenantAffected(tenantId, "PacketLoss").Should().BeTrue();
    }

    [Fact]
    public async Task InjectRandomLatency_ShouldSetLatencyRange()
    {
        var tenantId = "globex";
        var min = TimeSpan.FromMilliseconds(10);
        var max = TimeSpan.FromMilliseconds(100);

        await _engine.InjectRandomLatencyAsync(tenantId, min, max);

        _engine.IsTenantAffected(tenantId, "RandomLatency").Should().BeTrue();
    }

    [Fact]
    public async Task InjectDeadlock_ShouldEnableDeadlock()
    {
        var tenantId = "initech";

        await _engine.InjectDeadlockAsync(tenantId);

        _engine.IsTenantAffected(tenantId, "Deadlock").Should().BeTrue();
    }

    [Fact]
    public async Task InjectConnectionExhaustion_ShouldEnableExhaustion()
    {
        var tenantId = "acme";

        await _engine.InjectConnectionExhaustionAsync(tenantId);

        _engine.IsTenantAffected(tenantId, "ConnectionExhaustion").Should().BeTrue();
    }

    [Fact]
    public void ClearChaosState_ShouldRemoveState()
    {
        var tenantId = "acme";
        _engine.InjectDbOutageAsync(tenantId).Wait();

        _engine.ClearChaosState(tenantId);

        _engine.IsTenantAffected(tenantId, "DbOutage").Should().BeFalse();
    }

    [Fact]
    public void GetAffectedTenants_ShouldReturnAffectedList()
    {
        var tenants = new[] { "acme", "globex", "initech" };
        foreach (var tenant in tenants)
        {
            _engine.InjectDbOutageAsync(tenant).Wait();
        }

        var affected = _engine.GetAffectedTenants();

        affected.Should().HaveCount(3);
        affected.Keys.Should().Contain("acme");
        affected.Keys.Should().Contain("globex");
        affected.Keys.Should().Contain("initech");
    }
}

public sealed class TenantLoadGeneratorTests
{
    private readonly TenantChaosEngine _chaosEngine;
    private readonly TenantLoadGenerator _generator;

    public TenantLoadGeneratorTests()
    {
        _chaosEngine = new TenantChaosEngine(NullLogger<TenantChaosEngine>.Instance);
        _generator = new TenantLoadGenerator(NullLogger<TenantLoadGenerator>.Instance, _chaosEngine);
    }

    [Fact]
    public async Task GenerateLoadAsync_ShouldProcessRequests()
    {
        var tenantCount = 3;
        var duration = TimeSpan.FromMilliseconds(500);

        await _generator.GenerateLoadAsync(tenantCount, duration);

        _generator.TotalOperationsCompleted.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GenerateConcurrentPayrollLoadAsync_ShouldProcessPayroll()
    {
        var tenants = new List<string> { "acme", "globex" };
        var operations = 5;

        await _generator.GenerateConcurrentPayrollLoadAsync(tenants, operations);

        _generator.PayrollOperationsCompleted.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GenerateConcurrentAttendanceLoadAsync_ShouldProcessAttendance()
    {
        var tenants = new List<string> { "acme", "globex" };
        var operations = 5;

        await _generator.GenerateConcurrentAttendanceLoadAsync(tenants, operations);

        _generator.AttendanceOperationsCompleted.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ActiveTenantCount_ShouldReflectLoadScale()
    {
        _generator.ActiveTenantCount.Should().Be(0);
    }
}

public sealed class ReplayStormGeneratorTests
{
    private readonly ReplayStormGenerator _generator;

    public ReplayStormGeneratorTests()
    {
        _generator = new ReplayStormGenerator(NullLogger<ReplayStormGenerator>.Instance);
    }

    [Fact]
    public async Task GenerateReplayAttack_ShouldDetectReplays()
    {
        var tenantId = "acme";
        var messageIds = new List<string> { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() };
        var window = TimeSpan.FromMinutes(5);

        await _generator.GenerateReplayAttackAsync(tenantId, messageIds, window);

        var report = _generator.GetStormReport(tenantId);
        report.TotalReplaysDetected.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GenerateStaleDelivery_ShouldDeliverStale()
    {
        var tenantId = "globex";
        var messageIds = new List<string> { Guid.NewGuid().ToString() };
        var age = TimeSpan.FromHours(2);

        await _generator.GenerateStaleDeliveryAsync(tenantId, messageIds, age);

        var report = _generator.GetStormReport(tenantId);
        report.TotalStaleDeliveries.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GenerateDuplicateDelivery_ShouldTrackDuplicates()
    {
        var tenantId = "initech";
        var messageIds = new List<string> { Guid.NewGuid().ToString() };
        var duplicates = 3;

        await _generator.GenerateDuplicateDeliveryAsync(tenantId, messageIds, duplicates);

        var report = _generator.GetStormReport(tenantId);
        report.TotalDuplicateDeliveries.Should().Be(duplicates);
    }

    [Fact]
    public void GetStormReport_ShouldReturnCorrectStats()
    {
        var tenantId = "acme";
        var messageIds = new List<string> { Guid.NewGuid().ToString() };

        _generator.GenerateReplayAttackAsync(tenantId, messageIds, TimeSpan.FromMinutes(5)).Wait();

        var report = _generator.GetStormReport(tenantId);
        report.TenantId.Should().Be(tenantId);
        report.TotalMessagesProcessed.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ClearStormState_ShouldClearState()
    {
        var tenantId = "acme";
        var messageIds = new List<string> { Guid.NewGuid().ToString() };

        _generator.GenerateReplayAttackAsync(tenantId, messageIds, TimeSpan.FromMinutes(5)).Wait();
        _generator.ClearStormState(tenantId);

        var report = _generator.GetStormReport(tenantId);
        report.TotalMessagesProcessed.Should().Be(0);
    }
}

public sealed class RetryStormGeneratorTests
{
    private readonly RetryStormGenerator _generator;

    public RetryStormGeneratorTests()
    {
        _generator = new RetryStormGenerator(NullLogger<RetryStormGenerator>.Instance);
    }

    [Fact]
    public async Task GenerateRetryStorm_ShouldTrackRetries()
    {
        var tenantId = "acme";
        var operationIds = new List<string> { "op1", "op2" };
        var maxRetries = 3;

        await _generator.GenerateRetryStormAsync(tenantId, operationIds, maxRetries);

        var report = _generator.GetStormReport(tenantId);
        report.TotalRetriesAttempted.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GenerateRetryTimingAttack_ShouldTrackViolations()
    {
        var tenantId = "globex";
        var operationIds = new List<string> { "op1" };
        var backoff = TimeSpan.FromMilliseconds(100);
        var jitter = 0.5;

        await _generator.GenerateRetryTimingAttackAsync(tenantId, operationIds, backoff, jitter);

        var report = _generator.GetStormReport(tenantId);
        report.TotalRetriesAttempted.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GenerateExponentialBackoffFailure_ShouldDetectViolations()
    {
        var tenantId = "initech";
        var operationIds = new List<string> { "op1" };
        var immediateRetry = true;

        await _generator.GenerateExponentialBackoffFailureAsync(tenantId, operationIds, immediateRetry);

        var report = _generator.GetStormReport(tenantId);
        report.TotalBackoffViolations.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GenerateRetryContextCorruption_ShouldDetectCorruption()
    {
        var tenantId = "acme";
        var operationIds = new List<string> { "op1" };

        await _generator.GenerateRetryContextCorruptionAsync(tenantId, operationIds);

        var report = _generator.GetStormReport(tenantId);
        report.TotalContextCorruptions.Should().BeGreaterThan(0);
    }

    [Fact]
    public void GetStormReport_ShouldReturnCorrectStats()
    {
        var tenantId = "acme";
        var operationIds = new List<string> { "op1" };

        _generator.GenerateRetryStormAsync(tenantId, operationIds, 3).Wait();

        var report = _generator.GetStormReport(tenantId);
        report.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public void ClearStormState_ShouldClearState()
    {
        var tenantId = "acme";
        var operationIds = new List<string> { "op1" };

        _generator.GenerateRetryStormAsync(tenantId, operationIds, 3).Wait();
        _generator.ClearStormState(tenantId);

        var report = _generator.GetStormReport(tenantId);
        report.TotalRetriesAttempted.Should().Be(0);
    }
}
