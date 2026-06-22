# Outbox Runtime Certification — Pre-Sprint-3
## Phase A: Outbox Chaos Aggregate Evidence

| Field | Value |
|---|---|
| **Date** | _______________ |
| **Operator** | _______________ |
| **Sprint gate** | Pre-Sprint-3 |
| **Script** | `scripts/chaos/certify-outbox-chaos.sh` |
| **Prior certification** | `docs/audits/sprint-certification/OUTBOX_CERTIFICATION.md` (CONDITIONALLY CERTIFIED 2026-06-01) |

---

## What Was Previously Verified (Sprint 1–2)

| Scenario | Status |
|---|---|
| Broker (RabbitMQ) outage + restart, message preserved | VERIFIED (`scripts/chaos/broker-restart.sh`) |
| Worker crash + restart, tenant isolation preserved | VERIFIED (`scripts/runtime/d1-wire-proof.sh`) |
| Outbox implementation (UPDLOCK/READPAST, circuit breaker, retry) | UNIT TESTED (40 tests PASS) |

---

## Phase A Scenarios (This Certification Run)

| ID | Scenario | Evidence File | Verdict |
|---|---|---|---|
| A1 | Single hire → exactly 1 OutboxMessage row | [OUTBOX_CHAOS_A1.md](OUTBOX_CHAOS_A1.md) | _________ |
| A2 | 100-event burst → 100 OutboxMessage rows | [OUTBOX_CHAOS_A2.md](OUTBOX_CHAOS_A2.md) | _________ |
| A3 | Worker crash mid-flight → exactly 1 Employee row on restart | [OUTBOX_CHAOS_A3.md](OUTBOX_CHAOS_A3.md) | _________ |

---

## Aggregate SQL Evidence (Run After All Scenarios)

```sql
-- 1. OutboxMessage table health
SELECT COUNT(*) AS PendingOutboxMessages FROM dbo.OutboxMessage;

-- 2. OutboxProcessingState (retry tracker)
SELECT COUNT(*) AS InFlightStates,
       MAX(Attempt) AS MaxAttempt
FROM dbo.OutboxProcessingState;

-- 3. Dead-letter (must be 0 for clean run)
SELECT COUNT(*) AS DeadLetterCount FROM dbo.OutboxDeadLetter;

-- 4. Relay has drained everything (queue depth = 0 after drain window)
-- [verify via: curl http://guest:guest@localhost:15672/api/queues/%2F/EmployeeOnboarded]

-- 5. PayrollProfiles created per tenant (sanity check)
SELECT s.name AS SchemaName, COUNT(*) AS ProfileCount
FROM sys.tables t
JOIN sys.schemas s ON t.schema_id = s.schema_id
JOIN sys.partitions p ON p.object_id = t.object_id AND p.index_id IN (0,1)
WHERE t.name = 'PayrollProfiles'
GROUP BY s.name
ORDER BY s.name;
```

---

## Aggregate Results

| Metric | Observed | Expected |
|---|---|---|
| Pending OutboxMessages after full drain | ___________ | 0 |
| OutboxDeadLetter rows (clean run) | ___________ | 0 |
| Max retry attempt seen | ___________ | <= 3 |
| Relay circuit breaker trips | ___________ | 0 |

---

## Certification Decision

**[ ] CERTIFIED** — All A1, A2, A3 scenarios PASS. Outbox write-path and relay verified at runtime.

**[ ] CONDITIONALLY CERTIFIED** — Minor gaps noted below, no blocking defects.

**[ ] NOT CERTIFIED** — Blocking defects noted below.

Gaps / blockers:
_____________________________________________________________
_____________________________________________________________

Signed-off by: _______________  Date: _______________
