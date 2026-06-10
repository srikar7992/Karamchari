# Performance and Parallelization Review

Status as of 2026-06-10. Covers release-readiness items 14 (memory optimization) and 15
(parallelization review).

## Policy

1. **Profile before optimizing.** No Span/ArrayPool/CollectionsMarshal changes land
   without a benchmark or profile showing the call site is hot. Tools: BenchmarkDotNet
   (in-repo, below), dotMemory, PerfView.
2. **No parallelism inside request handlers.** Request throughput comes from ASP.NET
   Core's own concurrency; intra-request fan-out adds thread-pool pressure for little gain.
3. **EF Core DbContext is not thread-safe.** Any parallel work must give each unit of
   work its own DI scope (and therefore its own DbContext). The recompute job below is
   the reference implementation.

## Benchmark harness (item 14)

`tests/Backend/Karamchari.Benchmarks` — BenchmarkDotNet console project, in the solution.

```powershell
dotnet run -c Release --project tests/Backend/Karamchari.Benchmarks
```

Current coverage: `BurnoutScoreCalculator` (single typical, single high-risk, and a
5,000-employee tenant sweep with MemoryDiagnoser). The Intelligence scoring calculators
are pure static classes (`Services/Scoring/`), so adding more benchmarks is mechanical.
Add a benchmark before optimizing any calculator; commit the before/after numbers in
the PR description.

No memory optimizations have been applied yet because no profile has shown a hotspot.
This is deliberate (see policy 1).

## Parallelization inventory (item 15)

Reviewed all `Parallel.ForEachAsync` / `Task.WhenAll` / `Channel` usage in src/Backend.

| Site | Pattern | Verdict |
|---|---|---|
| `Forecasting/Services/WorkforceRecomputeJob.cs` | `Parallel.ForEachAsync` over tenants, per-tenant `CreateAsyncScope`, `MaxDegreeOfParallelism = 4` | Correct. The reference pattern: scope-per-tenant avoids cross-thread DbContext access; DOP cap bounds connection-pool pressure. |
| `Payroll/Consumers/PayrollBatchConsumer.cs` | `Task.WhenAll` over `context.Publish` of completion events | Correct. Publish fan-out is I/O-bound and context-free; DB writes around it stay sequential on the single DbContext. |
| `Core/Messaging/Outbox/OutboxRelayService.cs` | `Task.WhenAll` in relay dispatch | Correct: concurrent bus publishes, sequential DB bookkeeping. |
| `Core/Chaos/Tenant/TenantLoadGenerator.cs` | `Task.WhenAll` load fan-out | Test/chaos tooling; not production path. |
| BFF endpoints (`PayrollCockpitEndpoints`, `ComplianceGateEndpoints`, `OpsEndpoints`) | Single `Task.WhenAll` each | Acceptable: small fixed fan-out of independent reads. Watch rule: never share one DbContext across the awaited tasks — these use separate query sources. |

### Candidates evaluated and NOT parallelized

- **Nightly Intelligence scoring sweeps** — per-employee calculator calls are
  microseconds of pure CPU; the cost is the surrounding EF query, not the loop. Parallelizing
  the loop would multiply DbContext instances for no measured gain. Re-evaluate if the
  tenant sweep benchmark (above) shows the calculation itself dominating.
- **Projection consumers** — MassTransit already provides per-consumer concurrency;
  adding inner parallelism risks out-of-order event application within a projection.
- **Request handlers** — excluded by policy 2.

### Follow-up triggers

Revisit this document when any of these happen:
- A tenant exceeds ~50k employees (scoring sweep may become CPU-bound).
- Projection rebuild wall-time exceeds the nightly window (raise recompute DOP first —
  it is a one-line change — before restructuring).
- NBomber runs (`tests/Backend/Karamchari.PerformanceTests`) show GC pressure; then
  profile with dotMemory and add benchmarks for the implicated paths.
