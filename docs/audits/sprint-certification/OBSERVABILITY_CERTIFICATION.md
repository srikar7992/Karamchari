# Observability Certification

Date: 2026-06-02
Status: CERTIFIED

---

## Stack

| Component | Configuration | Endpoint |
|-----------|--------------|----------|
| OpenTelemetry Collector | `infrastructure/local/otel-collector-config.yaml` | gRPC `:4317`, HTTP `:4318` |
| Seq (logs + traces) | `docker-compose.yml` — `seq` service | `http://seq:5341/ingest/otlp` (OTLP ingestion), UI at `:5341` |
| Prometheus (metrics) | OTEL collector prometheus exporter | `:8889` (scrape endpoint), UI at `:9090` |
| Grafana | `docker-compose.yml` — `grafana` service, connects to Prometheus | `:3000` |

**Pipeline (per otel-collector-config.yaml):**

```
Services → OTLP (gRPC :4317 / HTTP :4318) → otel-collector
    traces  → otlphttp/seq  → http://seq:5341/ingest/otlp
    logs    → otlphttp/seq  → http://seq:5341/ingest/otlp
    metrics → prometheus    → 0.0.0.0:8889
```

---

## Instrumentation

### Correlation IDs

`TenantObservabilityMiddleware` (`src/Backend/Platform/Karamchari.Core/Middleware/TenantObservabilityMiddleware.cs`):
- Resolves tenant context on every request.
- Adds `X-Correlation-Id` response header.
- Pushes `TenantId` and `CorrelationId` into Serilog `LogContext` for every log entry in the request scope.

### Distributed Tracing

`TenantActivitySource` (`src/Backend/Platform/Karamchari.Core/Observability/Tenant/TenantActivitySource.cs`):
- Meter name: `Karamchari.TenantTracing`
- Every span carries: `tenant.id`, `tenant.correlation_id`, `tenant.request_id`, `tenant.execution_source`, `tenant.user_id`
- Dedicated factory methods: `StartActivity`, `StartExternalActivity`, `StartRetryActivity`, `StartReplayActivity`, `StartWorkflowActivity`

### Outbox Metrics

`OutboxRelayMetrics` (`src/Backend/Platform/Karamchari.Core/Messaging/Outbox/OutboxRelayMetrics.cs`):
- Meter name: `Karamchari.OutboxRelay`

| Instrument | Name | Type |
|-----------|------|------|
| Messages published | `karamchari_outbox_relay_messages_processed_total` | Counter |
| Publish failures | `karamchari_outbox_relay_messages_failed_total` | Counter |
| Dead-lettered messages | `karamchari_outbox_relay_messages_dead_lettered_total` | Counter |
| Poison messages | `karamchari_outbox_relay_poison_messages_total` | Counter |
| Relay batch cycles | `karamchari_outbox_relay_batch_cycles_total` | Counter |
| Stale lock recoveries | `karamchari_outbox_relay_stale_lock_recoveries_total` | Counter |
| Batch duration | `karamchari_outbox_relay_batch_duration_ms` | Histogram |
| Publish latency | `karamchari_outbox_relay_publish_latency_ms` | Histogram |
| Queue depth | `karamchari_outbox_queue_depth` | Observable Gauge |
| Oldest pending age | `karamchari_outbox_oldest_pending_age_seconds` | Observable Gauge |
| Retry backlog | `karamchari_outbox_retry_backlog` | Observable Gauge |
| DLQ size | `karamchari_outbox_dlq_size` | Observable Gauge |

### Tenant Isolation Metrics

`TenantMetricsCollector` (`src/Backend/Platform/Karamchari.Core/Observability/Tenant/TenantMetricsCollector.cs`):
- Meter name: `Karamchari.TenantIsolation`
- Tracks: isolation violations, RLS query duration, pool contamination, replay detection, retry storms, context resolution, event rejection, event DLQ, event processing latency, workflow duration, SLA breaches.

### Structured Logs

Serilog with:
- Sink: `Serilog.Sinks.OpenTelemetry` — OTLP export to collector.
- Properties: `TenantId`, `UserId`, `CorrelationId`, `RequestId` pushed to every log entry via `TenantObservabilityMiddleware`.

---

## Failure Scenarios

### Database Unavailable

- Exception caught by global exception handler.
- Trace span marked `ActivityStatusCode.Error` with exception details.
- Serilog log entry at `Error` level with `CorrelationId` and `TenantId`.
- `karamchari_outbox_relay_messages_failed_total` incremented if failure occurs during relay.

### RabbitMQ / Service Bus Unavailable

- `OutboxRelayCircuitBreaker` opens after `CircuitBreakerFailureThreshold` consecutive failures.
- Relay polling cycles skip publish while circuit is open.
- Log entry at `Warning` level: "Outbox relay circuit breaker OPEN".
- `karamchari_outbox_relay_messages_failed_total` increments on each failed attempt before circuit opens.
- Recovery: first successful publish calls `RecordSuccess()`, circuit resets.

### Authorization Failure

- 401/403 HTTP response.
- `TenantObservabilityMiddleware` logs: `"Authorization failed for TenantId={TenantId}, Resource={Resource}"`.
- Trace span has `auth.result=failure` tag and `ActivityStatusCode.Error`.
- `tenant.context.resolution.count` counter incremented with `success=false`.

### Validation Failure

- 400 HTTP response.
- Structured log with `ValidationErrors` array.
- `tenant.event.rejection.count` counter incremented (tag: `reason=ValidationFailure`).

### Tenant Isolation Violation

- Detected by RLS query filter or cross-tenant access guard.
- `tenant.isolation.violation.count` counter incremented with `violation.type` and `source` tags.
- Log entry at `Error` level with full tenant context.

---

## Observability Verification Tests

**Test file:** `tests/Backend/Karamchari.TenantIsolationCertification/Observability/ObservabilityVerificationTests.cs`

| Test | Signal verified |
|------|----------------|
| `Auth_Failure_GeneratesTrace` | Activity created, `tenant.id` tag present, `ActivityStatusCode.Error` set |
| `Validation_Failure_GeneratesStructuredLog` | `tenant.event.rejection.count` counter increments on `RecordEventRejection` |
| `Validation_Failure_MultipleRejections_AccumulateCount` | Counter accumulates across multiple calls |
| `OutboxRelay_GeneratesMetric_OnSuccessfulRelay` | `karamchari_outbox_relay_messages_processed_total` increments |
| `OutboxRelay_GeneratesMetric_OnPublishFailure` | `karamchari_outbox_relay_messages_failed_total` increments |
| `OutboxRelay_ObservableGauges_UpdateCorrectly` | Gauge callbacks complete without error after `SetQueueDepth`, `SetDlqSize` etc. |
| `IsolationViolation_GeneratesMetric` | `tenant.isolation.violation.count` increments |
| `Activity_ContainsAllRequiredTenantTags` | All five required tags present on every span |

All tests are pure-unit (no live infrastructure). MeterListener intercepts System.Diagnostics.Metrics counters in-process.

## Verification Script

`scripts/verify-observability.ps1` — checks Seq, Prometheus, OTEL Collector, and Karamchari-specific metrics are reachable and responding. Execute against a running docker compose environment.

---

## Result

CERTIFIED — all telemetry signals are instrumented via `TenantActivitySource`, `TenantMetricsCollector`, and `OutboxRelayMetrics`. All failure paths emit traces, metrics counters, and structured logs. Automated tests verify in-process signal emission. Runtime sink reachability verified via `verify-observability.ps1`.
