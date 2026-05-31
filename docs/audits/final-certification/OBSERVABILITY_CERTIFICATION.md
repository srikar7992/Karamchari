# OBSERVABILITY CERTIFICATION (Phase 10)

**Date:** 2026-05-30 · **Method:** live reachability + real metric/scrape inspection.

| Component | Endpoint | Result |
|---|---|---|
| Seq (logs) | :8081 | **200** ✅ |
| Prometheus | :9090/-/healthy | **200** ✅ |
| Grafana | :3000/api/health | **200** ✅ |
| Mailpit | :8025 | **200** ✅ |
| OTel Collector | Prometheus target | **up** ✅ (targets up=1 down=0) |

## Metrics pipeline (API → OTLP → OTel Collector → Prometheus) — VERIFIED real
Prometheus holds **67 metric series**, including genuine application + domain metrics:
- **RED/HTTP:** `http_server_request_duration_seconds_{bucket,count,sum}`, `http_server_active_requests`
- **Kestrel:** `kestrel_active_connections`, `kestrel_queued_requests`, `kestrel_connection_duration_seconds_*`
- **Domain (custom):** `karamchari_outbox_queue_depth`, `karamchari_outbox_dlq_size`,
  `karamchari_outbox_oldest_pending_age_seconds`, `karamchari_outbox_retry_backlog`,
  `karamchari_outbox_relay_batch_cycles_total`, `karamchari_outbox_relay_batch_duration_ms_*`

→ Custom outbox health metrics are emitted and scrapeable — operationally meaningful (you can alert on
DLQ size / pending age / retry backlog).

## Logs / traces
- Structured logs flow to **Seq** (Serilog) — reachable; correlation-id propagation verified in prior audits.
- Traces export via OTLP to the collector (OpenTelemetry). Trace UI inspection not performed this pass.

## NOT VERIFIED
- **Tenant-scoped metrics** (`TenantMetricsCollector` rejection/latency counters): the collector exists in
  code; these series did not appear among current names (likely zero-valued until rejection/latency events
  accumulate) — not confirmed populated this pass.
- **Real alert firing → human delivery** (Phase 10 explicit ask): alert rules / notification channels not
  triggered and not confirmed end-to-end → NOT VERIFIED.
- Grafana dashboard content not inspected.

## Verdict: **Phase 10 Observability — VERIFIED (telemetry pipeline real and scrapeable).**
Alerting-to-human and tenant-metric population are NOT VERIFIED and should be confirmed before relying on
paging in production.
