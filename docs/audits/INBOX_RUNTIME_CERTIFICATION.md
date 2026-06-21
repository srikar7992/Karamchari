# Inbox Runtime Certification — Pre-Sprint-3
## Phase B: Inbox Replay Aggregate Evidence

| Field | Value |
|---|---|
| **Date** | _______________ |
| **Operator** | _______________ |
| **Sprint gate** | Pre-Sprint-3 |
| **Script** | `scripts/chaos/certify-inbox-replay.sh` |
| **Prior certification** | `docs/audits/sprint-certification/INBOX_CERTIFICATION.md` (CONDITIONALLY CERTIFIED 2026-06-01) |

---

## What Was Previously Verified (Sprint 1–2)

| Component | Status |
|---|---|
| MassTransit `InboxState` configured in `dbo` schema | VERIFIED (KaramchariDbContext + HRDbContext) |
| Domain-level idempotency: ProcessedEventLog (Billing) | ARCHITECTURE VERIFIED |
| Domain-level idempotency: ExternalRevisionId (Compensation) | TEST PASS |
| Domain-level idempotency: IdempotencyKey (Notifications) | TEST PASS |
| Domain-level idempotency: LearningEnrollment dedup | TEST PASS |
| Runtime dedup (100 deliveries of same MessageId) | NOT YET RUN |

---

## Phase B Scenarios (This Certification Run)

| ID | Scenario | Key Assertion | Verdict |
|---|---|---|---|
| B1 | 100x identical MessageId → exactly 1 InboxState / 1 side effect | `COUNT(InboxState WHERE MessageId=X) = 1` | _________ |
| B2 | 50 concurrent deliveries of same MessageId → 1 InboxState record | No duplicate rows under race | _________ |
| B3 | Worker restart mid-replay → no duplicate PayrollProfile | `COUNT(PayrollProfiles WHERE EmployeeId=X) <= 1` | _________ |

---

## B1 Evidence

**MessageId used:** `______________________________________`  
**EmployeeNumber:** `______________________________________`

| Check | Observed | Pass/Fail |
|---|---|---|
| Messages published to queue | 100 | — |
| InboxState rows for this MessageId | ___________ | _________ |
| PayrollProfiles for this EmployeeId | ___________ | _________ |

```sql
-- InboxState deduplication record
SELECT
    CONVERT(varchar(36), MessageId) AS MessageId,
    Received, Delivered, Consumed,
    LastSequenceNumber
FROM dbo.InboxState
WHERE MessageId = '<b1-message-id>';

-- Side-effect: exactly one profile
SELECT COUNT(*) FROM tenant_dev.PayrollProfiles
WHERE EmployeeId = '<b1-employee-id>';
```

**B1 Verdict:** **[ ] PASS** / **[ ] FAIL**

---

## B2 Evidence

**MessageId used:** `______________________________________`  
**EmployeeNumber:** `______________________________________`

| Check | Observed | Pass/Fail |
|---|---|---|
| Concurrent publishes | 50 | — |
| InboxState rows for this MessageId | ___________ | _________ |
| PayrollProfiles for this EmployeeId | ___________ | _________ |

```sql
SELECT
    CONVERT(varchar(36), MessageId) AS MessageId,
    Received, Delivered, Consumed
FROM dbo.InboxState
WHERE MessageId = '<b2-message-id>';

SELECT COUNT(*) FROM tenant_dev.PayrollProfiles
WHERE EmployeeId = '<b2-employee-id>';
```

**B2 Verdict:** **[ ] PASS** / **[ ] FAIL**

---

## B3 Evidence

**MessageId used:** `______________________________________`  
**EmployeeNumber:** `______________________________________`

| Check | Observed | Pass/Fail |
|---|---|---|
| Copies published before kill | 10 | — |
| Queue depth when worker killed | ___________ | — |
| PayrollProfiles BEFORE restart | ___________ | _________ |
| Worker reconnect time (seconds) | ___________ | — |
| PayrollProfiles AFTER restart + drain (expect <= 1) | ___________ | _________ |
| Dead-letter entries (expect 0) | ___________ | _________ |

```sql
-- Side effects must not duplicate
SELECT COUNT(*) FROM tenant_dev.PayrollProfiles
WHERE EmployeeId = '<b3-employee-id>';

-- InboxState post-drain
SELECT
    CONVERT(varchar(36), MessageId) AS MessageId,
    Received, Delivered, Consumed
FROM dbo.InboxState
WHERE MessageId = '<b3-message-id>';

-- Dead-letter check
SELECT COUNT(*) FROM dbo.OutboxDeadLetter
WHERE Body LIKE '%<b3-employee-number>%';
```

**B3 Verdict:** **[ ] PASS** / **[ ] FAIL**

---

## Aggregate SQL Evidence (Run After All Scenarios)

```sql
-- InboxState table health (recent entries)
SELECT TOP 20
    CONVERT(varchar(36), MessageId) AS MessageId,
    Received,
    Delivered,
    Consumed,
    LastSequenceNumber
FROM dbo.InboxState
ORDER BY Received DESC;

-- Verify no unexpected dead-letter entries
SELECT COUNT(*) AS TotalDeadLetters FROM dbo.OutboxDeadLetter;

-- PayrollProfiles per tenant (sanity — no inflated counts)
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
| Total InboxState rows after all scenarios | ___________ | Low (MassTransit may GC consumed rows) |
| Total dead-letter rows | ___________ | 0 |
| Duplicate PayrollProfiles detected | ___________ | 0 |

---

## Important Notes on InboxState Behavior

MassTransit may remove `InboxState` rows after confirmed delivery (depending on configuration).
A count of 0 after processing is acceptable — the absence of **duplicate side effects** is
the primary assertion. Always verify `PayrollProfiles` counts in addition to `InboxState`.

---

## Certification Decision

**[ ] CERTIFIED** — All B1, B2, B3 scenarios PASS. Inbox deduplication verified at runtime.

**[ ] CONDITIONALLY CERTIFIED** — Minor gaps noted below, no blocking defects.

**[ ] NOT CERTIFIED** — Blocking defects noted below.

Gaps / blockers:
_____________________________________________________________
_____________________________________________________________

Signed-off by: _______________  Date: _______________
