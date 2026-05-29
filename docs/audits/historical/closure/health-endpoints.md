# WS9 — Health & Kubernetes Readiness

**Status: ✅ CLOSED (was MEDIUM).**

## Changes implemented (`HealthCheckExtensions.cs`)
- `/health/live` — liveness (predicate `_ => false`, always 200 if the process is up).
- `/health/ready` — readiness (checks tagged `ready`: 14 DbContexts + RabbitMQ + Redis).
- `/health/startup` — **added**: startup probe over the `ready` checks, with `Degraded/Unhealthy → 503`.
- `/health` — aggregate (unchanged).

## Verification
```
/health/live    -> HTTP 200
/health/ready   -> HTTP 200
/health/startup -> HTTP 200
```
Dependency-failure behavior (validated in Phase 16 chaos and re-confirmed): stopping Redis or RabbitMQ flips the dependency-bearing endpoints to 503 while `/health/live` stays 200 — so an orchestrator will not kill a live pod during a transient dependency blip.

## Verdict
Health & Kubernetes Readiness = **PASS** — liveness/readiness/startup split in place.
