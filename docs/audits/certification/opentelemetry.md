# Phase 11 — OpenTelemetry Certification

**Result: ⚠️ PARTIAL.** Telemetry is configured and emitted by the app, but no collector was running this session to assert receipt; one instrumentation gap noted.

## Configuration (code-verified)
- **Logs:** Serilog `WriteTo.OpenTelemetry` → OTLP endpoint `OTEL_EXPORTER_OTLP_ENDPOINT ?? http://localhost:4317`, resource attrs `service.name=Karamchari.Api`, `deployment.environment=<env>` (`LoggingExtensions.cs`).
- **Traces / Metrics:** OTel tracing/metrics registration lives in `InfrastructureExtensions`/Core observability; W3C trace context is active (see below).

## Live evidence
- **Activity correlation / HTTP tracing:** error responses carry a valid W3C `traceId`, e.g. `"traceId":"00-60ed8499c59fffcf32b7820c5a07e772-0d2ccf8d635ff176-01"` — confirms an `Activity` is created per request and propagated. ✅
- **EF Core tracing:** EF command activities are present (DbCommand log entries with durations correlate to the request scope). ✅ (instrumentation active)
- **Tenant observability:** `TenantObservabilityMiddleware` + `TenantTelemetryTags` enrich activities/log scope with `TenantId`/`CorrelationId`.

## Gaps / not asserted
- **No OTLP collector running** this session (`otel-collector` not started), so I could not assert traces/metrics were *received & stored*. The exporter targets `localhost:4317`; with nothing listening the export fails silently (no crash). To fully certify, start `otel-collector` + Prometheus + a trace backend and assert spans land.
- **MassTransit tracing / Redis tracing:** not independently confirmed as emitted spans in this session (no collector to inspect). MassTransit and StackExchange.Redis both support OTel instrumentation; presence of explicit `AddSource`/instrumentation registration for these should be confirmed against the collector.

## Verdict
Tracing is genuinely active (W3C trace IDs, EF activities). End-to-end OTel certification (traces/metrics for HTTP+EF+MassTransit+Redis landing in a backend) is **PARTIAL** pending a running collector. Re-run with the full observability stack up.
