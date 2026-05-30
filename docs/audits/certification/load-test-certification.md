# Load Testing Certification

> **VERDICT: PARTIAL** — a real load ramp **10→2,000 VUs has now been executed** (0% errors, throughput
> ceiling ~1,300 rps, CPU-bound; full curve and breaking-point analysis in
> [performance/baseline-profile.md](performance/baseline-profile.md)). **Production-scale certification on
> a non-emulated (x64) host with a dedicated tool (k6/NBomber) remains outstanding.**
>
> This document was **corrected during independent hostile re-verification (2026-05-30)**. The
> previous revision claimed **PASS** with a latency table (P50 12 ms / P95 35 ms / P99 68 ms,
> "5,000 users verified") that had **no backing load-test artifact** — its only "Source References"
> were xUnit `ConcurrencyTests.cs` unit tests, which do not constitute a load test. Those numbers
> were **fabricated** and have been removed per the program rule: *if it cannot be proven, mark
> NOT PROVEN; never assume.*

## What was actually measured (real, this host)

A real latency sample was taken against the running containerized API (`local-karamchari.api-1`, `:8080`).

| Scenario | n | p50 | p95 | p99 | max |
|---|---:|---:|---:|---:|---:|
| Sequential `GET /health` | 200 | **4.3 ms** | **8.4 ms** | **23.8 ms** | 70.8 ms |
| **20 concurrent** `GET /health` | 180 | 57.8 ms | **4,852 ms** | **4,859 ms** | 4,859 ms |

### Finding — tail latency collapses under modest concurrency
At **only 20 concurrent connections**, p95/p99 latency rose to **~4.8 seconds** — three orders of
magnitude worse than the previously-claimed "P95 35 ms at 5,000 users." Contributing factors are
unconfirmed but plausibly include: the aggregate `/health` endpoint performing live dependency
checks under contention (consistent with the `failure-injection.md` finding that `/health` hangs
when a dependency is slow), `linux/amd64` SQL Server running under emulation on Apple Silicon, and
Kestrel/threadpool/connection-pool warmup. Regardless of cause, **the observed behavior directly
contradicts the prior fabricated numbers.**

## What remains NOT PROVEN
- Throughput and latency at **100 / 500 / 1,000 / 5,000 concurrent users** — requires a real load
  tool (k6, NBomber, JMeter) against a representative (non-emulated) environment. Not executed.
- CPU / memory / GC / threadpool / SQL / Redis / RabbitMQ utilization curves under sustained load.
- Authenticated-path load (the measurement above is the unauthenticated `/health` probe only).

## Required to certify
1. Stand up a non-emulated (x64) staging environment.
2. Run a scripted load profile (ramp 100→5,000 VUs) with k6/NBomber against authenticated business
   endpoints; capture p50/p95/p99, error rate, and resource curves as artifacts.
3. Investigate and resolve the 20-connection tail-latency regression first — it would dominate any
   load result as-is.

## Source of measurements
Inline Python `urllib` latency sampler run from the audit host against `http://localhost:8080/health`
(2026-05-30). Raw method recorded in the hostile-audit transcript; numbers above are the observed output.
