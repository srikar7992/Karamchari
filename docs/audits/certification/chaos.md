# Phase 16 — Chaos Certification

**Result: ✅ PASS (dependency-outage detection & recovery).** App survives Redis/RabbitMQ outages and self-heals; one design note on health granularity.

## Health composition (baseline)
`GET /health` = `Healthy`, aggregating: 14 `Database:*` checks + `Caching:Redis` + `Messaging:RabbitMQ` + `masstransit-bus`.

## Experiments
| Action | `/health` | App process | Recovery |
|---|---|---|---|
| `docker stop local-redis-1` | **503** | stayed up | — |
| `docker start local-redis-1` | **200** | — | ✅ auto-recovered (no app restart) |
| `docker stop local-rabbitmq-1` | **503** | stayed up | — |
| `docker start local-rabbitmq-1` | **200** | — | ✅ auto-recovered |
| SQL retry policy | configured | — | `AddKaramchariResilience()` + EF execution strategy (`ExecuteAsync`/retry) observed in stack traces; MassTransit `UseMessageRetry(3 × 5s)` |

The process did not crash through either outage; health correctly flipped to 503 and back to 200 on restart.

## Findings
- **MEDIUM — No liveness/readiness split.** A single `/health` returns 503 if **any** dependency is down. Under Kubernetes, wiring this to a *liveness* probe would cause the orchestrator to **kill an otherwise-alive pod** during a transient Redis/RabbitMQ blip. Expose separate `/health/live` (process up) and `/health/ready` (dependencies up) endpoints.
- **INFO:** SQL restart-retry was validated indirectly via the EF execution-strategy + resilience registration and message retry; a full "stop SQL mid-write, observe retry success" was not run to avoid corrupting the provisioned DB mid-session.

## Verdict
Resilience to dependency outages is genuine and verified (detect → degrade → recover). Add liveness/readiness separation before production.
