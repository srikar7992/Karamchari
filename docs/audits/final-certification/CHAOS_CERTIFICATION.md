# CHAOS CERTIFICATION (Phase 9)

**Date:** 2026-05-30 · **Method:** real fault injection against running containers.
Repro: `scripts/chaos/broker-restart.sh`, `scripts/runtime/d1-wire-proof.sh`.

| Fault | Behaviour during | Recovery | Post-recovery correctness | Verdict |
|---|---|---|---|---|
| **RabbitMQ outage** | `health/ready` → **503** (dependency-aware, fail-loud) | broker mgmt up 6s; **worker reconsuming ~30s (RTO)** | new `dev` onboard → `tenant_dev` (+1); **`dbo` unchanged**; **no message loss (RPO≈0)** | ✅ VERIFIED |
| **Worker crash/restart** | queue buffers (`messages=1, consumers=0`); no loss | reconnect + drain ~24s cold | parked `dev` message consumed into `tenant_dev` correctly | ✅ VERIFIED |
| SQL outage | — | — | — | ⏳ NOT TESTED this pass (prior failure-injection evidence exists) |
| Redis outage | — | — | — | ⏳ NOT TESTED this pass |
| API crash / node restart | — | — | — | ⏳ NOT TESTED this pass |
| Network delay / packet loss | — | — | — | ⏳ NOT TESTED this pass |

## RTO / RPO (measured, this pass)
- **Broker RTO ≈ 30s** (bus reconnect + endpoint redeclaration); cold worker start ≈ 24s.
- **RPO ≈ 0** for committed work — the EF outbox + durable queues guarantee at-least-once delivery;
  no committed onboarding was lost across broker restart.

## Key resilience property confirmed
**Tenant isolation is preserved across infrastructure failure.** After both worker restart and broker
restart, async side-effects continued landing in the correct `tenant_*` schema with zero `dbo`/`system`
leakage. The D1 fix is not fragile to messaging chaos.

## NOT VERIFIED
- SQL/Redis/API/node outages and network-fault injection were not run this pass → NOT VERIFIED.
- Duplicate-delivery / replay-rejection live test (ReplayProtectionService + InboxState) — code present and
  active (worker polls `InboxState`), but a forced duplicate was not injected → NOT VERIFIED.
- Poison-message → DLQ routing not explicitly triggered (Enforce-mode rejection path exists in code).

## Verdict: **Phase 9 — VERIFIED for messaging chaos (broker + worker); other faults NOT VERIFIED.**
The two highest-risk messaging faults recover correctly and preserve isolation. A full chaos matrix
(DB/cache/network/node) remains for the human-driven cycle.
