# Outbox Chaos Evidence — Scenario A2
## 100-Event Burst → 100 OutboxMessage Rows

| Field | Value |
|---|---|
| **Date** | _______________ |
| **Operator** | _______________ |
| **Environment** | local (docker compose, infrastructure/local) |
| **Script** | `scripts/chaos/certify-outbox-chaos.sh` (Scenario A2 block) |

---

## Scenario

Fire 100 sequential POST requests to `POST /api/v1/hr/employees/` while the worker is stopped.
Every successful hire must produce exactly one `dbo.OutboxMessage` row, with no rows silently
dropped under load. This validates the outbox write path under burst conditions.

The worker is stopped before the burst so that:
- Messages accumulate in `dbo.OutboxMessage` (not immediately relayed and deleted).
- The count comparison is stable (the relay cannot drain rows during measurement).

After the burst, the worker is restarted and the count is verified before drain begins.

---

## Steps

1. Record baseline `SELECT COUNT(*) FROM dbo.OutboxMessage`.
2. Stop the worker container (`local-karamchari.worker-1`).
3. Register one test user under tenant `dev`.
4. Fire 100 POST requests to `/api/v1/hr/employees/` (sequential to respect rate limits).
5. Record how many returned HTTP 201/200 (successful hires).
6. Wait 2 seconds.
7. Record final `SELECT COUNT(*) FROM dbo.OutboxMessage`.
8. Assert delta >= number of successful hires (ideally exactly equal).
9. Restart worker.

---

## Expected

- At least 95/100 hires return HTTP 201.
- `dbo.OutboxMessage` count increases by exactly the number of successful hires.
- No silent drops: every successful API call writes one outbox row.

---

## Actual

| Check | Observed | Pass/Fail |
|---|---|---|
| Successful hires (HTTP 201/200) | _________ / 100 | _________ |
| Failed hires (non-201) | _________ / 100 | — |
| OutboxMessage count BEFORE burst | ___________ | — |
| OutboxMessage count AFTER burst | ___________ | — |
| Delta (expected = successful hires) | ___________ | _________ |

---

## SQL Queries Used

```sql
-- Baseline count
SELECT COUNT(*) FROM dbo.OutboxMessage;

-- Post-burst aggregate (recent 5 minutes)
SELECT
    COUNT(*)       AS TotalOutboxMessages,
    MIN(SentTime)  AS Earliest,
    MAX(SentTime)  AS Latest
FROM dbo.OutboxMessage
WHERE SentTime >= DATEADD(MINUTE, -5, GETUTCDATE());

-- Message type breakdown (all should be EmployeeOnboarded)
SELECT MessageType, COUNT(*) AS Cnt
FROM dbo.OutboxMessage
WHERE SentTime >= DATEADD(MINUTE, -5, GETUTCDATE())
GROUP BY MessageType;
```

---

## Observations

Worker restart after burst:
- Worker reconnect time (seconds): ___________
- Queue depth before drain: ___________
- Any messages in dead-letter after drain: ___________

---

## Verdict

**[ ] PASS** / **[ ] FAIL**

Notes:
_____________________________________________________________
_____________________________________________________________
