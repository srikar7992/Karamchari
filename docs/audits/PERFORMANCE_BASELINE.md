# Performance Baseline — Phase C Certification

**Date:** _FILL IN_
**Tester:** _FILL IN_
**Environment:** `docker compose up` local dev (Apple Silicon / x64 — record host)
**API build:** `docker images karamchari-api --format "{{.ID}}"` → _FILL IN_
**Scripts:** `scripts/perf/certify-performance-p1.sh`, `scripts/perf/certify-performance-p2.sh`, `scripts/perf/load-ramp.py`

---

## P1 — Auth Endpoint: 100 Concurrent Users

**Endpoint:** `POST /api/identity/login`
**Command:** `bash scripts/perf/certify-performance-p1.sh --vus 100 --duration 30`
**Criterion:** P95 < 200ms, error% < 5%

| VUs | Requests | Throughput (rps) | Error% | P50 (ms) | P95 (ms) | P99 (ms) | Max (ms) |
|-----|----------|-----------------|--------|----------|----------|----------|----------|
| 100 | _FILL_   | _FILL_          | _FILL_ | _FILL_   | _FILL_   | _FILL_   | _FILL_   |

**Verdict:** `PASS / FAIL`
**Notes:** _FILL IN — e.g. rate-limit 429s counted as errors? host CPU%?_

---

## P2 — Full Recruitment Journey: 10 Concurrent Journeys

**Journey:** requisition → candidate → application → interview → offer → approve → issue → hire
**Command:** `bash scripts/perf/certify-performance-p2.sh --journeys 10`
**Criterion:** median journey < 5000ms, 0 failed journeys

| Journeys | Failures | Median (ms) | P95 (ms) | P99 (ms) | Max (ms) |
|----------|----------|-------------|----------|----------|----------|
| 10       | _FILL_   | _FILL_      | _FILL_   | _FILL_   | _FILL_   |

**Per-step breakdown (median across journeys):**

| Step | Median (ms) |
|------|------------|
| create_requisition | _FILL_ |
| create_candidate   | _FILL_ |
| create_application | _FILL_ |
| schedule_interview | _FILL_ |
| create_offer       | _FILL_ |
| approve_offer      | _FILL_ |
| issue_offer        | _FILL_ |
| hire_candidate     | _FILL_ |

**Verdict:** `PASS / FAIL`
**Notes:** _FILL IN_

---

## P3 — Tenant Scale (Aspirational — Not Automated)

**Scope:** 100 tenants, 10k requisitions, 50k candidates — query perf + tenant isolation
**Status:** NOT VERIFIED in scripts — requires production-scale data seeding
**Evidence location:** _FILL IN if manually run_

---

## P0 — GET /employees Baseline (retained from Phase 8)

From `scripts/perf/load-ramp.py` and `PERFORMANCE_CERTIFICATION.md`:

| VUs  | Requests | Throughput (rps) | Error% | P50 (ms) | P95 (ms) | P99 (ms) |
|------|----------|-----------------|--------|----------|----------|----------|
| 10   | —        | —               | 0%     | —        | —        | —        |
| 100  | —        | —               | 0%     | —        | —        | —        |
| 250  | —        | ~1358           | 0%     | —        | —        | —        |
| 500  | —        | —               | 0%     | —        | —        | —        |
| 1000 | —        | —               | 0%     | —        | —        | —        |
| 2000 | —        | —               | 0%     | —        | —        | —        |

P95 for GET /employees at 100 VUs: **7ms** (Phase 8 certified)

---

## Resource utilization during P1 / P2

| Test | CPU (API container) | CPU (SQL container) | Mem (API) | Notes |
|------|--------------------|--------------------|-----------|-------|
| P1 auth 100 VUs | _FILL_ | _FILL_ | _FILL_ | |
| P2 10 journeys  | _FILL_ | _FILL_ | _FILL_ | |

_CPU captured via `docker stats --no-stream`_

---

## Honest scope

- All results are host-bound (single Apple Silicon or x64 dev machine under docker).
- Absolute throughput is NOT a production SLO — it establishes shape and relative regression.
- Production capacity sizing requires representative x64 hardware with dedicated DB.
- P3 (100-tenant scale) requires a separate data seeding pass and is flagged as NOT VERIFIED until executed.

---

## Certification sign-off

| Item | Result | Tester | Date |
|------|--------|--------|------|
| P1: auth 100 VUs P95 < 200ms | PASS / FAIL | | |
| P1: error% < 5% | PASS / FAIL | | |
| P2: 10 concurrent journeys, 0 failures | PASS / FAIL | | |
| P2: median journey < 5s | PASS / FAIL | | |
| P0: GET /employees retained (Phase 8) | PASS | srikarbojji | 2026-05-30 |

**Overall Phase C Verdict:** `PASS / FAIL`
