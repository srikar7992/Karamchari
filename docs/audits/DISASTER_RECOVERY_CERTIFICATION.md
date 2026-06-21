# Disaster Recovery Certification — Phase F

**Date:** _FILL IN_
**Tester:** _FILL IN_
**Environment:** `docker compose up` local dev
**SQL container:** `local-sqlserver-1`
**Script:** `scripts/dr/certify-disaster-recovery.sh`

---

## F1 — Database Backup and Restore

**Method:** `BACKUP DATABASE` inside SQL container → `RESTORE DATABASE` to separate DB → row count comparison
**Command:** `bash scripts/dr/certify-disaster-recovery.sh --skip-f3`

### Pre-backup row counts

| Table (schema.name) | Row count |
|--------------------|-----------|
| _FILL IN_ | _FILL_ |
| _FILL IN_ | _FILL_ |
| _FILL IN_ | _FILL_ |
| _(add all non-empty tables)_ | |

### Backup execution

| Step | Expected | Actual | Result |
|------|----------|--------|--------|
| `BACKUP DATABASE` to `/var/backups/karamchari_cert_*.bak` | Completes successfully | _FILL_ | PASS/FAIL |
| Backup file present in container | File exists with size > 0 | _FILL_ size=_FILL_ | PASS/FAIL |
| Backup duration | < 60s | _FILL_ s | PASS/FAIL |

### Restore execution

| Step | Expected | Actual | Result |
|------|----------|--------|--------|
| `RESTORE DATABASE KaramchariRestore_Cert` with REPLACE | Completes successfully | _FILL_ | PASS/FAIL |
| Row counts match original DB | 100% match | MATCH / MISMATCH | PASS/FAIL |

### Post-restore row counts (restored DB)

| Table (schema.name) | Original | Restored | Delta |
|--------------------|---------|----------|-------|
| _FILL IN_ | _FILL_ | _FILL_ | 0 |
| _FILL IN_ | _FILL_ | _FILL_ | 0 |
| _(add all tables)_ | | | |

**RTO (restore time):** _FILL IN seconds_
**RPO (data loss):** 0 rows (point-in-time backup; no transactions between backup and restore)

**F1 Verdict:** `PASS / FAIL`

---

## F2 — Worker Event Replay / Message Recovery

**Status:** Verified in CHAOS_CERTIFICATION.md (Phase 9)
**Reference script:** `scripts/chaos/broker-restart.sh` and `scripts/runtime/d1-e2e-proof.sh`

**Evidence (from CHAOS_CERTIFICATION.md):**
- RabbitMQ broker stopped → API continued serving HTTP
- Broker restarted → worker reconnected within observed seconds
- Parked events were consumed; PayrollProfiles row counts matched expected +1 per tenant
- Tenant isolation maintained post-recovery

| Check | Result |
|-------|--------|
| Events produced during outage are persisted in queue | VERIFIED |
| Worker reconnects after broker restart | VERIFIED |
| Events replayed with correct tenant isolation | VERIFIED |
| No cross-tenant contamination post-recovery | VERIFIED |

**Re-run command:** `bash scripts/runtime/d1-e2e-proof.sh`

**F2 Verdict:** `PASS` (previously certified — re-run if required for this sprint)

---

## F3 — Full Environment Rebuild

**WARNING: DESTRUCTIVE — runs `docker compose down -v` (destroys all volumes)**
**Command:** `bash scripts/dr/certify-disaster-recovery.sh` (without `--skip-f3`)

| Step | Expected | Actual | Result |
|------|----------|--------|--------|
| `docker compose down -v` | All containers + volumes removed | _FILL_ | PASS/FAIL |
| `docker compose up -d --build` | All containers healthy | _FILL_ | PASS/FAIL |
| API `/health/live` returns 200 | Within 120s of startup | _FILL_ s | PASS/FAIL |
| `admin@dev.local` login succeeds | Token returned | _FILL_ | PASS/FAIL |
| `GET /api/v1/hr/employees/` returns 200 | Authed request succeeds | _FILL_ | PASS/FAIL |

**Startup time (compose up → healthy):** _FILL IN seconds_

**Migration evidence:** Dev data seeder ran (admin@dev.local login succeeds) → migrations applied automatically on startup.

**F3 Verdict:** `PASS / FAIL`

---

## Full Run Output

```
# Paste output of: bash scripts/dr/certify-disaster-recovery.sh [--skip-f3]
# (replace this block with actual terminal output)
FILL IN
```

---

## Recovery objectives summary

| Metric | Target | Measured | Result |
|--------|--------|----------|--------|
| RTO (backup restore) | < 10 min | _FILL_ | PASS/FAIL |
| RTO (full rebuild) | < 15 min | _FILL_ | PASS/FAIL |
| RPO (backup restore) | 0 rows lost | 0 rows lost | PASS/FAIL |
| RPO (worker replay) | 0 events lost | 0 events lost (queue-durable) | PASS |

---

## Certification sign-off

| Phase | Checks | Pass | Fail | Verdict | Tester | Date |
|-------|--------|------|------|---------|--------|------|
| F1: DB backup | 2 | _FILL_ | _FILL_ | PASS/FAIL | | |
| F1: DB restore | 2 | _FILL_ | _FILL_ | PASS/FAIL | | |
| F1: row count match | 1 | _FILL_ | _FILL_ | PASS/FAIL | | |
| F2: worker event replay | reference | 4 | 0 | PASS | — | 2026-05-30 |
| F3: full rebuild | 5 | _FILL_ | _FILL_ | PASS/FAIL | | |

**Overall Phase F Verdict:** `PASS / FAIL`
