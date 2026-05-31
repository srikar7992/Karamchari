# PERFORMANCE CERTIFICATION (Phase 8)

**Date:** 2026-05-30 · **Method:** real measurement; no synthetic numbers.

## Latency sample on rebuilt API (post D1-fix, with HMAC signing path active)
`GET /api/v1/hr/employees/` (authed, n=50, light concurrency), API `3d47a2d7…`:

| p50 | p90 | p95 | p99 | max |
|---|---|---|---|---|
| **5 ms** | 7 ms | **7 ms** | 18 ms | 18 ms |

→ The execution-context signing added on the publish path causes **no measurable read-path regression**;
business endpoint is single-digit ms at p95.

## Load curve (prior real ramp, retained — `scripts/perf/load-ramp.py`, baseline-profile.md)
Closed-loop VU ramp 10→2000 VUs against the containerized API:
- **0% errors at every level** (10/100/250/500/1000/2000).
- Throughput peak **~1358 rps @ ~250 VUs**; beyond that, CPU-bound (API container ~412% CPU).
- Latency grows **linearly** with VUs past saturation (Little's Law) — **no crash cliff**, graceful degradation.
- Health-check latency fix verified prior: `/health/ready` p95 3825 ms → 22 ms (~113×); `/health` 6539→7 ms.

## Honest scope
- The 10–2000 ramp was executed on this single Apple-Silicon host (SQL Server under amd64 emulation);
  **absolute throughput is host-bound and not a production SLO** — it establishes shape (linear, CPU-bound,
  no cliff), not capacity numbers for prod hardware. → Production capacity sizing = NOT VERIFIED (needs
  representative x64 hardware).
- Worker async throughput, DB-latency-under-load, and queue-latency-under-load: NOT separately profiled
  this pass.

## Verdict: **Phase 8 Performance — VERIFIED (local).**
Latency healthy, degradation graceful, zero errors to 2000 VUs. Production capacity numbers require a
representative host (NOT VERIFIED here).
