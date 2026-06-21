# OBSERVABILITY CERTIFICATION
## Karamchari Pre-Sprint-3 — Phase G

**Date:** 2026-06-02  
**Supersedes:** docs/audits/final-certification/OBSERVABILITY_CERTIFICATION.md (Phase 10, 2026-05-30)  
**Method:** Live reachability + metric/scrape inspection + structured log evidence + gap inventory

---

## Telemetry Stack Reachability

| Component | Endpoint | Verified? | Evidence |
|---|---|---|---|
| Seq (structured logs) | :8081 | YES | HTTP 200 — FULL_PLATFORM_EVIDENCE_MATRIX.md; AUTH events captured live |
| Prometheus | :9090/-/healthy | YES | HTTP 200 — FINAL_PLATFORM_READINESS_BOARD_REPORT.md |
| Grafana | :3000/api/health | YES | HTTP 200 — FINAL_PLATFORM_READINESS_BOARD_REPORT.md |
| Mailpit | :8025 | YES | HTTP 200 — OBSERVABILITY_CERTIFICATION Phase 10 |
| OTel Collector | Prometheus target | YES | targets up=1 down=0 confirmed |
| OTLP Ingest (gRPC) | :4317 | YES | API → OTel collector pipeline proven (metrics reach Prometheus) |

---

## Metrics Pipeline (API → OTLP → OTel Collector → Prometheus)

**Total metric series:** 67 (verified live, 2026-05-30)

| Metric Category | Series | Verified? | Evidence |
|---|---|---|---|
| HTTP RED: `http_server_request_duration_seconds_{bucket,count,sum}` | 3 | YES | Live Prometheus scrape |
| HTTP RED: `http_server_active_requests` | 1 | YES | Live Prometheus scrape |
| Kestrel: `kestrel_active_connections` | 1 | YES | Live Prometheus scrape |
| Kestrel: `kestrel_queued_requests` | 1 | YES | Live Prometheus scrape |
| Kestrel: `kestrel_connection_duration_seconds_*` | 2 | YES | Live Prometheus scrape |
| Domain custom: `karamchari_outbox_queue_depth` | 1 | YES | Live Prometheus scrape |
| Domain custom: `karamchari_outbox_dlq_size` | 1 | YES | Live Prometheus scrape |
| Domain custom: `karamchari_outbox_oldest_pending_age_seconds` | 1 | YES | Live Prometheus scrape |
| Domain custom: `karamchari_outbox_retry_backlog` | 1 | YES | Live Prometheus scrape |
| Domain custom: `karamchari_outbox_relay_batch_cycles_total` | 1 | YES | Live Prometheus scrape |
| Domain custom: `karamchari_outbox_relay_batch_duration_ms_*` | 2 | YES | Live Prometheus scrape |
| Tenant-scoped rejection/latency counters (`TenantMetricsCollector`) | ? | NO | Collector exists in code; series not confirmed populated — requires rejection/latency events to accumulate |

---

## Structured Logging (Seq)

| Log Category | Verified? | Evidence |
|---|---|---|
| Auth success events (`AUTH_SUCCESS`) | YES | Live capture: `AUTH_SUCCESS Email=admin@dev.local Tenant=dev` — AUTHENTICATION_CERTIFICATION.md |
| Auth failure events (`AUTH_FAILURE`) | YES | Live capture: `AUTH_FAILURE Reason=Invalid credentials` — AUTHENTICATION_CERTIFICATION.md |
| Multi-tenant auth events (separate tenants) | YES | Live capture: dev + acme login events both present in Seq — FULL_PLATFORM_EVIDENCE_MATRIX.md |
| Correlation ID propagation | PARTIAL | Prior audits note correlation-id propagation verified; distributed trace UI inspection not performed this pass |
| Worker / outbox relay logs | YES | Worker container logs observed active; outbox relay polling confirmed |
| Structured error logs on DB failure | NOT VERIFIED | DB kill test not executed; behavior expected but not observed live |
| RabbitMQ unavailable — degraded-state log | NOT VERIFIED | Broker-stop test not run against Seq; chaos cert confirmed broker recovery behavior but not log content |
| Unhandled exception with correlation ID | NOT VERIFIED | No unhandled exception injection performed this pass |
| Authorization failure audit log entry | NOT VERIFIED | Auth failure logged (AUTH_FAILURE) but dedicated authorization audit-trail entry not separately verified |

---

## Distributed Trace Correlation

| Item | Verified? | Evidence |
|---|---|---|
| OTLP trace export from API | YES | OTel collector receives from API (pipeline proven via metric delivery) |
| Trace UI (Grafana / Jaeger) | NOT VERIFIED | Grafana dashboard content not inspected; no trace visualization confirmed |
| Correlation ID end-to-end (HTTP → outbox → worker) | NOT VERIFIED | MT-Correlation-Id and MT-Conversation-Id present on wire messages (ASYNC_CERTIFICATION.md) but not traced in observability UI |
| Correlation ID in Seq log entries | PARTIAL | Seq receives structured logs; correlation-id field presence in log schema not directly confirmed this pass |

---

## Alerting Pipeline

| Item | Verified? | Evidence |
|---|---|---|
| Prometheus scrape config exists | YES | targets up=1 confirmed |
| Alert rules defined | NOT VERIFIED | Alert rule files not inspected this pass |
| Alert → Alertmanager → notification channel | NOT VERIFIED | End-to-end alert firing to human not triggered |
| PagerDuty / email / Slack notification on alert | NOT VERIFIED | No notification channel configured or tested |

---

## NOT VERIFIED — Forced Failure Scenarios

The following scenarios are defined but not yet executed. The certification script `scripts/observability/certify-observability-forced-failures.sh` covers each of these:

| Scenario | Expected Observable | Verification Method | Status |
|---|---|---|---|
| DB unavailable (`docker stop sqlserver`) → API call | Seq: structured Error log with SqlException | Steps 1 in cert script | NOT VERIFIED |
| RabbitMQ unavailable → async trigger | Prometheus: `karamchari_outbox_queue_depth` increases | Step 2 in cert script | NOT VERIFIED |
| Invalid credentials × N | Seq: Warning-level AUTH_FAILURE entries | Step 3 in cert script | NOT VERIFIED |
| No auth token on protected endpoint | Seq: structured 401 / authorization log | Step 3 in cert script | NOT VERIFIED |
| Unhandled exception | Seq: Error with correlation ID | Inject 500 via test endpoint | NOT VERIFIED |
| Service recovery post-failure | Metrics return to normal; API returns 200 | Step 5 in cert script | NOT VERIFIED |
| Tenant-scoped metrics populate | `TenantMetricsCollector` series visible in Prometheus | Generate cross-tenant rejection events | NOT VERIFIED |

---

## Verdict by Layer

| Layer | Verdict | Gaps Remaining |
|---|---|---|
| Reachability (Seq, Prom, Grafana) | VERIFIED | None |
| Metrics pipeline (OTLP → Prometheus) | VERIFIED | Tenant-scoped series not confirmed populated |
| Custom domain metrics (outbox_*) | VERIFIED | Series exist and are scrapeable |
| Structured auth logging (Seq) | VERIFIED | Correlation ID field presence not confirmed |
| Alerting to human | NOT VERIFIED | Alert rules, channels, and firing not tested |
| Forced-failure observable behavior | NOT VERIFIED | DB-down, broker-down, auth-failure log entries not observed live |
| Distributed trace correlation end-to-end | NOT VERIFIED | Trace UI not inspected; correlation ID propagation through observability stack unconfirmed |
| Grafana dashboard content | NOT VERIFIED | Dashboard panels not inspected |

---

## Certification Decision

**PARTIAL — Telemetry infrastructure is real and operational. Observable behavior under failure conditions has not been verified.**

To achieve CERTIFIED status, execute `scripts/observability/certify-observability-forced-failures.sh` against the live stack and record passing output for each step.

**Alert-to-human pipeline is a production prerequisite and must be verified before any on-call responsibility is assigned.**
