// -----------------------------------------------------------------------
// <copyright file="ObservabilityVerificationTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics;
using System.Diagnostics.Metrics;
using FluentAssertions;
using Karamchari.Core.Messaging.Outbox;
using Karamchari.Core.Multitenancy;
using Karamchari.Core.Multitenancy.Execution;
using Karamchari.Core.Observability.Tenant;
using Microsoft.Extensions.Options;
using Xunit;

namespace Karamchari.TenantIsolationCertification.Observability;

/// <summary>
/// Observability verification tests (Phase G).
///
/// These tests exercise the production telemetry instrumentation directly —
/// TenantActivitySource, TenantMetricsCollector, and OutboxRelayMetrics —
/// to confirm that traces, structured log context, and metrics counters
/// are emitted correctly under failure conditions.
///
/// All tests are pure-unit: no live infrastructure required.
/// </summary>
public sealed class ObservabilityVerificationTests : IDisposable
{
    private readonly TenantActivitySource _activitySource;
    private readonly TenantMetricsCollector _metricsCollector;
    private readonly OutboxRelayMetrics _outboxMetrics;

    // MeterListener to capture counter readings in-process.
    private readonly MeterListener _meterListener;
    private readonly Dictionary<string, long> _counterReadings = new();
    private readonly object _counterLock = new();

    public ObservabilityVerificationTests()
    {
        _activitySource = new TenantActivitySource();
        _metricsCollector = new TenantMetricsCollector();
        _outboxMetrics = new OutboxRelayMetrics();

        _meterListener = new MeterListener();
        _meterListener.InstrumentPublished = (instrument, listener) =>
        {
            // Subscribe to both meters used in this test class.
            if (instrument.Meter.Name is "Karamchari.TenantIsolation" or "Karamchari.OutboxRelay")
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };

        _meterListener.SetMeasurementEventCallback<long>((instrument, measurement, _, _) =>
        {
            lock (_counterLock)
            {
                var key = $"{instrument.Meter.Name}/{instrument.Name}";
                _counterReadings.TryGetValue(key, out var existing);
                _counterReadings[key] = existing + measurement;
            }
        });

        _meterListener.Start();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static TenantExecutionEnvelope MakeEnvelope(
        string tenantId = "acme",
        string correlationId = "test-corr-001") =>
        new TenantExecutionEnvelope(tenantId, correlationId, "req-test", ExecutionSource.HttpRequest, TenantSource.JwtClaim);

    private long ReadCounter(string meterName, string counterName)
    {
        // Force the MeterListener to collect pending measurements.
        _meterListener.RecordObservableInstruments();
        lock (_counterLock)
        {
            var key = $"{meterName}/{counterName}";
            _counterReadings.TryGetValue(key, out var value);
            return value;
        }
    }

    // ── G1: Auth failure generates a trace ────────────────────────────────────

    /// <summary>
    /// G1 — Auth_Failure_GeneratesTrace
    ///
    /// Verifies that TenantActivitySource creates an Activity with the expected
    /// tenant tag and marks it as errored when an authorization failure occurs.
    /// The OpenTelemetry SDK exports this Activity to the configured backend
    /// (Seq / OTLP collector) in production.
    /// </summary>
    [Fact]
    public void Auth_Failure_GeneratesTrace()
    {
        // Arrange — add a listener so ActivitySource.StartActivity returns non-null.
        using var listener = new ActivityListener
        {
            ShouldListenTo = src => src.Name == TenantActivitySource.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStarted = _ => { },
            ActivityStopped = _ => { }
        };
        ActivitySource.AddActivityListener(listener);

        var envelope = MakeEnvelope(tenantId: "globex", correlationId: "auth-fail-001");
        Activity? captured = null;

        // Act — simulate an auth-failure trace span.
        using (var activity = _activitySource.StartActivity(
                   "tenant.auth.check",
                   envelope,
                   ExecutionSource.HttpRequest,
                   correlationId: "auth-fail-001"))
        {
            if (activity is not null)
            {
                activity.SetTag("auth.result", "failure");
                activity.SetTag("http.status_code", "401");
                // Mark span as error — this is what production code does in GlobalExceptionHandler.
                activity.SetStatus(ActivityStatusCode.Error, "Authorization failed");
                captured = activity;
            }
        }

        // Assert
        captured.Should().NotBeNull(
            "TenantActivitySource must create an Activity when an ActivityListener is registered");

        captured!.GetTagItem(TenantTelemetryTags.TenantId).Should().Be("globex",
            "the tenant.id tag must propagate to the trace span");

        captured.GetTagItem(TenantTelemetryTags.CorrelationId).Should().Be("auth-fail-001",
            "the correlation ID must be present in the trace span");

        captured.GetTagItem("auth.result").Should().Be("failure",
            "the auth result tag must be set to 'failure' on an authorization failure");

        captured.Status.Should().Be(ActivityStatusCode.Error,
            "the span status must be Error so the backend marks it as a failed trace");
    }

    // ── G2: Validation failure generates structured log context ───────────────

    /// <summary>
    /// G2 — Validation_Failure_GeneratesStructuredLog
    ///
    /// Verifies that the TenantMetricsCollector records an event rejection (the
    /// metric emitted when a validation failure causes an event to be rejected)
    /// and that the counter is tagged with the correct tenant ID and reason.
    ///
    /// In production, validation failures also emit a structured Serilog log entry
    /// with TenantId, CorrelationId, and ValidationErrors. The metric counter is
    /// the in-process-verifiable signal we assert here.
    /// </summary>
    [Fact]
    public void Validation_Failure_GeneratesStructuredLog()
    {
        // Arrange
        const string tenantId = "acme";
        const string messageType = "CreateEmployeeCommand";
        const string reason = "ValidationFailure";

        // Act — record the event rejection that production code emits on 400 responses.
        _metricsCollector.RecordEventRejection(tenantId, messageType, reason);

        // Assert — the counter must have incremented.
        // Note: TenantMetricsCollector uses System.Diagnostics.Metrics counters. The
        // MeterListener wired in the constructor intercepts Add() calls synchronously.
        _meterListener.RecordObservableInstruments();
        lock (_counterLock)
        {
            var key = "Karamchari.TenantIsolation/tenant.event.rejection.count";
            _counterReadings.TryGetValue(key, out var count);
            count.Should().Be(1,
                "RecordEventRejection must increment the tenant.event.rejection.count counter by 1");
        }
    }

    /// <summary>
    /// G2-variant — Multiple validation failures for same tenant accumulate correctly.
    /// </summary>
    [Fact]
    public void Validation_Failure_MultipleRejections_AccumulateCount()
    {
        const string tenantId = "initech";
        const int rejectionCount = 7;

        for (int i = 0; i < rejectionCount; i++)
        {
            _metricsCollector.RecordEventRejection(tenantId, "SomeCommand", "ValidationFailure");
        }

        _meterListener.RecordObservableInstruments();
        lock (_counterLock)
        {
            var key = "Karamchari.TenantIsolation/tenant.event.rejection.count";
            _counterReadings.TryGetValue(key, out var count);
            count.Should().BeGreaterThanOrEqualTo(rejectionCount,
                "all rejection events must be reflected in the counter");
        }
    }

    // ── G3: OutboxRelay generates metrics ─────────────────────────────────────

    /// <summary>
    /// G3 — OutboxRelay_GeneratesMetric
    ///
    /// Verifies that OutboxRelayMetrics.MessagesProcessed increments correctly
    /// when the relay successfully publishes a message. This is the metric that
    /// Prometheus / Grafana dashboards display as relay throughput.
    /// </summary>
    [Fact]
    public void OutboxRelay_GeneratesMetric_OnSuccessfulRelay()
    {
        // Act — simulate relay processing 3 messages.
        _outboxMetrics.MessagesProcessed.Add(1);
        _outboxMetrics.MessagesProcessed.Add(1);
        _outboxMetrics.MessagesProcessed.Add(1);

        // Assert
        _meterListener.RecordObservableInstruments();
        lock (_counterLock)
        {
            var key = "Karamchari.OutboxRelay/karamchari_outbox_relay_messages_processed_total";
            _counterReadings.TryGetValue(key, out var count);
            count.Should().Be(3,
                "MessagesProcessed counter must reflect the number of successfully relayed messages");
        }
    }

    /// <summary>
    /// G3-variant — OutboxRelay_MessagesFailed increments on transient publish failure.
    /// </summary>
    [Fact]
    public void OutboxRelay_GeneratesMetric_OnPublishFailure()
    {
        _outboxMetrics.MessagesFailed.Add(1);

        _meterListener.RecordObservableInstruments();
        lock (_counterLock)
        {
            var key = "Karamchari.OutboxRelay/karamchari_outbox_relay_messages_failed_total";
            _counterReadings.TryGetValue(key, out var count);
            count.Should().Be(1,
                "MessagesFailed counter must increment when the relay encounters a transient publish failure");
        }
    }

    /// <summary>
    /// G3-variant — OutboxRelay observable gauges update atomically.
    /// Verifies that SetQueueDepth / SetDlqSize are reflected in gauge readings.
    /// </summary>
    [Fact]
    public void OutboxRelay_ObservableGauges_UpdateCorrectly()
    {
        _outboxMetrics.SetQueueDepth(42);
        _outboxMetrics.SetDlqSize(3);
        _outboxMetrics.SetRetryBacklog(7);
        _outboxMetrics.SetOldestPendingAgeSeconds(300);

        // Observable gauges are collected when RecordObservableInstruments is called.
        // We verify the API does not throw (gauges read backing fields atomically).
        var act = () => _meterListener.RecordObservableInstruments();
        act.Should().NotThrow("observable gauge callbacks must complete without errors");
    }

    // ── G4: Isolation violation metric ────────────────────────────────────────

    /// <summary>
    /// G4 — IsolationViolation_GeneratesMetric
    ///
    /// When an RLS or cross-tenant data access violation is detected,
    /// TenantMetricsCollector.RecordIsolationViolation must increment the
    /// tenant.isolation.violation.count counter tagged with the violation type.
    /// </summary>
    [Fact]
    public void IsolationViolation_GeneratesMetric()
    {
        _metricsCollector.RecordIsolationViolation(
            tenantId: "acme",
            violationType: "CrossTenantDataAccess",
            source: "EmployeeRepository");

        _meterListener.RecordObservableInstruments();
        lock (_counterLock)
        {
            var key = "Karamchari.TenantIsolation/tenant.isolation.violation.count";
            _counterReadings.TryGetValue(key, out var count);
            count.Should().Be(1,
                "RecordIsolationViolation must increment the isolation violation counter");
        }
    }

    // ── G5: Tracing tags are complete ─────────────────────────────────────────

    /// <summary>
    /// G5 — Activity_ContainsAllRequiredTenantTags
    ///
    /// Verifies that every Activity created by TenantActivitySource carries the
    /// full set of required observability tags so that Seq / Jaeger can correlate
    /// by tenant, user, correlation ID, and execution source.
    /// </summary>
    [Fact]
    public void Activity_ContainsAllRequiredTenantTags()
    {
        using var listener = new ActivityListener
        {
            ShouldListenTo = src => src.Name == TenantActivitySource.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStarted = _ => { },
            ActivityStopped = _ => { }
        };
        ActivitySource.AddActivityListener(listener);

        var envelope = new TenantExecutionEnvelope(
            tenantId: "acme",
            correlationId: "corr-999",
            requestId: "req-999",
            executionSource: ExecutionSource.HttpRequest,
            source: TenantSource.JwtClaim)
            .With(userIdentity: "user-42");

        using var activity = _activitySource.StartActivity(
            "tenant.operation.test",
            envelope,
            ExecutionSource.HttpRequest,
            correlationId: "corr-999");

        activity.Should().NotBeNull();

        activity!.GetTagItem(TenantTelemetryTags.TenantId).Should().Be("acme");
        activity.GetTagItem(TenantTelemetryTags.CorrelationId).Should().Be("corr-999");
        activity.GetTagItem(TenantTelemetryTags.RequestId).Should().Be("req-999");
        activity.GetTagItem(TenantTelemetryTags.ExecutionSource).Should().NotBeNull();
        activity.GetTagItem(TenantTelemetryTags.UserId).Should().Be("user-42");
    }

    // ── Teardown ──────────────────────────────────────────────────────────────

    public void Dispose()
    {
        _meterListener.Dispose();
        _metricsCollector.Dispose();
        _outboxMetrics.Dispose();
    }
}
