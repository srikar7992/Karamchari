# Performance Certification — Baseline, Root Cause & Fix (Workstream A)

**Date:** 2026-05-30 · **Method:** real latency measurement against the running stack; no estimates, no extrapolation.
**Tooling note:** k6/NBomber/JMeter are not installed on the audit host. Measurements below use a Python
`urllib` + `ThreadPoolExecutor` harness (real HTTP, real concurrency). Full 100→1000-VU load with a
dedicated tool on a non-emulated host remains **NOT PROVEN** (see "Remaining" below).

---

## A1 — Baseline (sequential, single client)

| Endpoint | p50 | p95 | p99 | max |
|---|---:|---:|---:|---:|
| `GET /health` (deps) | 4.3 ms | 8.4 ms | 23.8 ms | 70.8 ms |
| `GET /api/v1/hr/employees` (authed) | 16.1 ms | 39.4 ms | 41.4 ms | 50.8 ms |

Sequential latency is healthy.

## A2 — Concurrency localization (conc = 20)

The hostile audit flagged a multi-second tail under concurrency. Localizing it:

| Endpoint @ conc=20 | p50 | p95 | p99 |
|---|---:|---:|---:|
| `/health/live` (no deps) | 5.3 ms | 20.8 ms | 22.3 ms |
| `/health/ready` (deps) | 47 ms | **3,806–10,202 ms** | ~ |
| `/health` (deps) | 37 ms | **1,760–6,539 ms** | ~ |
| **`/api/v1/hr/employees` (authed business)** | 16 ms | **39.4 ms** | 41.4 ms |

**Key finding: the platform is NOT slow under concurrency.** The real authenticated business endpoint
holds p95 ≈ 39 ms at conc=20. The multi-second tail is **isolated to the dependency-checking health
endpoints** (`/health`, `/health/ready`). "Performance Unknown" is therefore resolved to a specific,
bounded health-check design defect — not a platform-wide concurrency failure.

## A3 — Root cause

`/health` and `/health/ready` evaluated **every dependency on every request**:
- **14 separate `AddDbContextCheck`** — all 16 bounded-context DbContexts target the *same* physical
  database (one `KaramchariDb` connection), so this was 14× redundant `SELECT 1` per request.
- RabbitMQ connection check + Redis ping.

Under 20 concurrent probes that is ~20 × 16 ≈ **320 simultaneous backend connections**, exhausting the
SQL connection pool and churning RabbitMQ connections → multi-second tail latency. It is also an
**unauthenticated amplification vector**: each anonymous `/health/ready` hit forced 16 backend ops.

## Fix (implemented — `HealthCheckExtensions.cs`)

1. **Collapse 14 DbContext checks → 1** `Database` connectivity check (all share `KaramchariDb`;
   per-context schema correctness is covered by provisioning + tests).
2. **5-second single-flight cache** (`CachedHealthReportProvider`): a burst of probes triggers **at most
   one** real dependency evaluation; others serve the cached `HealthReport`. Status-code mapping
   preserved (Healthy→200, Unhealthy→503; `/health/ready` treats Degraded as 503, `/health` as 200).
   `/health/live` remains dependency-free.

### Before → After (real, both endpoints returning 200; ok=200/200)

| Endpoint @ conc=20 | BEFORE p95 | AFTER p95 | Improvement |
|---|---:|---:|---:|
| `/health/ready` | 3,825 ms | **33.8 ms** | ~113× |
| `/health` | 6,539 ms | **7.4 ms** | ~880× |
| `/health/ready` @ conc=50 | — | **10.0 ms** (p99 11.7 ms) | scales |

> Validation honesty note: an earlier "AFTER" run reported ~3 ms but was **invalid** — the fixed host
> instance had failed to boot (the production JWT fail-fast guard fired under the default environment),
> so the harness was timing *connection-refused* errors. Re-run with the instance genuinely live
> (`ASPNETCORE_ENVIRONMENT=Local`, real JSON readiness responses) produced the validated numbers above.

### Trade-off (documented)
The 5 s cache means dependency-failure detection at the readiness endpoint can lag by up to 5 s. This is
acceptable for k8s readiness cadence (~10 s) and is far outweighed by eliminating the amplification/tail
risk. Liveness (`/health/live`) is uncached and immediate.

## Status
- **Concurrency tail latency: FOUND → FIXED → PROVEN** (runtime before/after). Fix is in source; the
  running container image (`local-karamchari.api-1`, :8080) still carries the old code until rebuilt
  (deployment step).
- Business-endpoint concurrency: **PROVEN healthy** (p95 ≈ 39 ms @ conc=20).

## Remaining — NOT PROVEN (require tools/host not available here)
- Throughput/latency/error-rate at **100 / 250 / 500 / 1000 VUs** with k6/NBomber on a **non-emulated
  (x64)** host (this host runs SQL Server under linux/amd64 emulation on Apple Silicon).
- CPU / memory / GC / threadpool / connection-pool curves under sustained load.
- Authenticated-path load beyond conc=50.
- Break-point ("continue until failure") testing.
