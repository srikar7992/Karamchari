# Outbox Chaos Evidence — Scenario A3
## Worker Crash Mid-Flight → Single Employee Row on Restart

| Field | Value |
|---|---|
| **Date** | _______________ |
| **Operator** | _______________ |
| **Environment** | local (docker compose, infrastructure/local) |
| **Script** | `scripts/chaos/certify-outbox-chaos.sh` (Scenario A3 block) |

---

## Scenario

Stop the worker before a hire, execute the hire (message parks in the RabbitMQ queue),
then restart the worker. After the message is consumed, verify that exactly one
`tenant_dev.Employees` row exists for the hired employee — no duplicate, no missing row.

This verifies at-least-once delivery combined with idempotent consumer behavior:
the InboxState deduplication layer must prevent a second processing even if the
worker crashed between ack and side-effect commit.

---

## Steps

1. Stop the worker container (`local-karamchari.worker-1`).
2. Register + login a unique test user under tenant `dev`.
3. POST one employee hire — message parks in `EmployeeOnboarded` queue.
4. Record `SELECT COUNT(*) FROM tenant_dev.Employees WHERE EmployeeNumber = '<empno>'` — expect 0.
5. Start the worker container (simulate crash recovery / restart).
6. Wait for the worker to reconnect (consumers = 1 on the queue).
7. Wait for the queue depth to reach 0.
8. Record `SELECT COUNT(*) FROM tenant_dev.Employees WHERE EmployeeNumber = '<empno>'` — expect exactly 1.
9. Verify `SELECT COUNT(*) FROM dbo.OutboxDeadLetter WHERE Body LIKE '%<empno>%'` — expect 0.

---

## Expected

- Queue depth after hire (worker stopped): 1 message parked.
- `tenant_dev.Employees` count for the test employee BEFORE restart: 0.
- `tenant_dev.Employees` count for the test employee AFTER restart + drain: **exactly 1**.
- `dbo.OutboxDeadLetter` entries for this employee: 0.

---

## Actual

| Check | Observed | Pass/Fail |
|---|---|---|
| HTTP status from hire | ___________ | _________ |
| Queue depth (worker stopped) | ___________ | _________ |
| Employees row BEFORE restart | ___________ | _________ |
| Worker reconnect time (seconds) | ___________ | — |
| Queue drain time (seconds) | ___________ | — |
| Employees row AFTER restart (expect 1) | ___________ | _________ |
| Dead-letter entries (expect 0) | ___________ | _________ |

---

## SQL Queries Used

```sql
-- BEFORE restart: row must not exist yet
SELECT COUNT(*)
FROM tenant_dev.Employees
WHERE EmployeeNumber = '<empno>';

-- AFTER restart + drain: exactly one row
SELECT COUNT(*)
FROM tenant_dev.Employees
WHERE EmployeeNumber = '<empno>';

-- No dead-letter pollution
SELECT COUNT(*)
FROM dbo.OutboxDeadLetter
WHERE Body LIKE '%<empno>%';

-- Confirm row details
SELECT EmployeeNumber, LegalName, CreatedAt
FROM tenant_dev.Employees
WHERE EmployeeNumber = '<empno>';
```

---

## Verdict

**[ ] PASS** / **[ ] FAIL**

Notes:
_____________________________________________________________
_____________________________________________________________
