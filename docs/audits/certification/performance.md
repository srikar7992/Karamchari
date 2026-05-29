# Phase 15 — Performance Smoke Certification

**Result: ✅ PASS (smoke, unauthenticated `/health`).** No authenticated load possible (auth-blocked); results are for the health aggregate under a Debug build with full logging.

## Method
Target: `http://localhost:60463/health` (aggregates 14 DbContexts + Redis + RabbitMQ + bus on every call — a non-trivial endpoint).

| Run | Mode | Result | Throughput |
|---|---|---|---|
| 100 | sequential | 100 × 200, 0 errors | ~67 req/s, ~15 ms/req |
| 500 | concurrent (P=20) | 500 × 200, 0 errors | ~106 req/s |
| 1000 | — | Not run separately; 60-burst + 500-concurrent already 0-error | — |

## Observations
- **Error rate:** 0% across all runs.
- **Latency:** `/health` self-reports ~8 ms internal duration; end-to-end ~15 ms sequential.
- **Connection pool stability:** No pool-exhaustion or transient SQL errors during 500 concurrent calls hitting 14 DbContexts each → SQL connection pooling stable.
- **Memory growth:** Not instrumented this session (no metrics backend). Note the **HIGH** risk: `IssuerSigningKeyResolver` builds a new `ServiceProvider` per token validation (Phase 4/14) — this would cause severe memory growth under *authenticated* load, which could not be measured here.

## Caveats / what was NOT measured
- All numbers are from a **Debug** build with verbose logging — not representative of Release/prod throughput.
- No authenticated business-endpoint load (CRUD, payroll) — blocked by auth.
- No latency percentiles (p50/p95/p99), no sustained soak, no memory/GC tracking.

## Verdict
Health-path smoke is clean and stable. A real performance certification (authenticated CRUD, percentiles, soak, memory) is **pending the auth fix** and a Release-build run with metrics.
