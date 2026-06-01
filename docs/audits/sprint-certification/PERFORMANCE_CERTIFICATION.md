# Performance Certification
**Date:** 2026-06-01  
**Status:** NOT CERTIFIED (Infrastructure Required)

---

## Status

Performance certification requires a running stack. This document defines the targets and test plan.

## Performance Targets

| Metric | Target | Measurement Method |
|---|---|---|
| API P50 latency | < 100ms | k6 / NBomber load test |
| API P99 latency | < 500ms | k6 / NBomber load test |
| Outbox throughput | > 500 msg/s | OutboxRelayMetrics gauge |
| EF query (tenant-filtered) | < 10ms | EF command interceptor timing |
| Candidate search | < 50ms | Application query timing |

## OpenTelemetry Instrumentation

All metrics implemented:
- `OutboxRelayMetrics`: BatchCycles, MessagesProcessed, PublishLatencyMs, PoisonMessages, StaleLockRecoveries
- ASP.NET Core: request duration, active connections (via OpenTelemetry.Instrumentation.AspNetCore)
- EF Core: query duration (via OpenTelemetry.Instrumentation.Http)
- MassTransit: message processing time (built-in)

## Test Plan

1. `docker compose up` — start SQL Server + RabbitMQ
2. `dotnet run --project Karamchari.Api` — start API
3. Run k6 script: `k6 run scripts/perf-test.js --vus 50 --duration 60s`
4. Capture OutboxRelay throughput from metrics endpoint
5. Record P50/P99 from k6 output

## Certification Decision

**NOT CERTIFIED** — requires live infrastructure. Performance instrumentation is complete and ready to measure.
