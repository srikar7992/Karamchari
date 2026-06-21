# Outbox Chaos Evidence — Scenario A1
## Single Hire Produces One OutboxMessage Row

| Field | Value |
|---|---|
| **Date** | _______________ |
| **Operator** | _______________ |
| **Environment** | local (docker compose, infrastructure/local) |
| **Script** | `scripts/chaos/certify-outbox-chaos.sh` (Scenario A1 block) |

---

## Scenario

A single POST to `POST /api/v1/hr/employees/` (tenant = `dev`) must result in exactly one
row being written to `dbo.OutboxMessage` before the outbox relay picks it up.

This verifies that the `DomainEventDispatchInterceptor` (or equivalent SaveChanges interceptor)
atomically appends an outbox row on every employee hire — no row lost, no duplicate row.

---

## Steps

1. Record baseline `SELECT COUNT(*) FROM dbo.OutboxMessage`.
2. Register + login a unique test user under tenant `dev`.
3. POST one employee hire to `/api/v1/hr/employees/`.
4. Wait 2 seconds for EF SaveChanges to complete.
5. Record final `SELECT COUNT(*) FROM dbo.OutboxMessage`.
6. Assert delta == 1.

---

## Expected

- HTTP 201 (or 200) from the hire endpoint.
- `dbo.OutboxMessage` count increases by exactly 1.
- The new row's `MessageType` contains `EmployeeOnboardedIntegrationEvent`.

---

## Actual

| Check | Observed | Pass/Fail |
|---|---|---|
| HTTP status from hire | ___________ | _________ |
| OutboxMessage count BEFORE | ___________ | — |
| OutboxMessage count AFTER | ___________ | — |
| Delta (expected 1) | ___________ | _________ |
| MessageType of new row | ___________ | _________ |

---

## SQL Queries Used

```sql
-- Baseline count
SELECT COUNT(*) FROM dbo.OutboxMessage;

-- Post-hire count
SELECT COUNT(*) FROM dbo.OutboxMessage;

-- Inspect the new row
SELECT TOP 1
    CONVERT(varchar(36), MessageId) AS MessageId,
    MessageType,
    LEFT(Body, 120) AS BodyPreview,
    SentTime
FROM dbo.OutboxMessage
ORDER BY SentTime DESC;
```

---

## Verdict

**[ ] PASS** / **[ ] FAIL**

Notes:
_____________________________________________________________
_____________________________________________________________
