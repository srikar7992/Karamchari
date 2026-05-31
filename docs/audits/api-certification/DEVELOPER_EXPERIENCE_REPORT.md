# DEVELOPER EXPERIENCE REPORT (Phase 8)

**Date:** 2026-05-30 · Question: can a newly hired engineer go from clone → running → authenticated →
discovering → executing workflows → debugging → extending, **without tribal knowledge**, in **< 2 hours**?

## The path (each step verified reachable this pass)
| Step | Mechanism | Status |
|---|---|---|
| 1. Clone | git | ✅ |
| 2. Start platform | `./setup-local.sh` (one command: infra + migrate + provision + seed) | ✅ runs from source |
| 3. Login | `admin@dev.local` / `Dev@Pass123!` → `POST /api/identity/login` | ✅ **now documented + seeded** |
| 4. Discover APIs | Scalar at `/scalar/v1`; 170 ops, **per-endpoint auth locks** (fixed this pass) | ✅ |
| 5. Execute workflow | onboard employee → async payroll profile (works end-to-end) | ✅ (core journey) |
| 6. Debug code | standard .NET debug against Dockerized infra; structured logs in Seq; RFC7807 errors w/ traceId | ✅ tooling present |
| 7. Trace request/message/db/event | correlation+tenant IDs propagate; Seq logs; `karamchari_outbox_*` metrics; D1 evidence shows full HTTP→outbox→RabbitMQ→DB trace | ✅ traceable |
| 8. Develop a feature | clear module structure (16 BCs), ADRs, runbooks, conventions | ✅ documented |

## What changed this pass (the tribal-knowledge removers)
- **Before:** no default users (a dev had to `register` an arbitrary account and guess a tenant), no seeded
  business data, no per-endpoint auth signal in Scalar. These were undocumented prerequisites.
- **After:** 16 documented users (4 roles × dev/acme/contoso/globex), `AUTHENTICATION_GUIDE.md`, automatic
  seeding via `--provision-dev-tenants`, and Scalar lock icons. **The "first login" cliff is removed.**

## Time-to-first-successful-change — assessment
- The previously-blocking unknowns (how do I log in? what tenant? what password? which endpoints need auth?)
  are resolved by documentation + seeding. The happy-path (clone → setup-local → login → onboard an employee
  via Scalar → see it persist) is reproducible and well under 2 hours for the **working** modules.
- **Honest caveat:** an engineer exploring **leave-balances**, **approvals**, or **capability** will hit
  500/403 (GAP-1/GAP-2) and waste time. Those are real friction points until fixed. So "< 2h to a successful
  change" holds for HR/Identity/onboarding; it does **not** hold uniformly across all modules yet.
- **NOT VERIFIED:** an actual independent-human trial + a measured stopwatch were not performed (cannot be
  simulated by the agent). The path is *demonstrably unblocked*; the human timing remains to be observed.

## Verdict
**Developer onboarding is substantially de-risked and now largely tribal-knowledge-free for the core path.**
Remaining DX friction is concentrated in GAP-1 (modules that 500) and GAP-2 (permission 403s).
