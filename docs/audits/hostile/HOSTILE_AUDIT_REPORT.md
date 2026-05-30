# Karamchari — Hostile Independent Verification & Re-Certification Report

**Auditor stance:** external; hired to *disprove* prior certifications. **No PASS inherited.**
**Method:** clean-room execution, source inspection, negative/failure testing on the running system.
**Host:** macOS arm64, .NET SDK 10.0.300, Docker 29.5.2, bash 3.2.57, `pwsh` absent.
**Objective:** not "does it work" but **"can it silently fail?"** — and it could, in two material ways (both now closed).

> Phase evidence: `docs/hostile-audit/*.md`. Every verdict below is backed by a captured command, query, or source citation.

---

## Headline: two silent failures found (and fixed)

1. **The documented fresh-machine bootstrap silently required an undocumented environment variable.**
   `appsettings.Development.json` pinned `KaramchariDb` to `(localdb)\MSSQLLocalDB` (Windows-only). Following
   README/README-LOCAL verbatim on macOS/Linux, provisioning **crashed** with
   `System.PlatformNotSupportedException: LocalDB is not supported on this platform`. The prior "greenfield PASS"
   was only reachable because the operator had `ConnectionStrings__KaramchariDb` exported in their shell — a step
   present only in a *certification artifact*, not onboarding. **This is the hidden manual step the audit exists to find.**

2. **A production deploy could silently sign JWTs with a publicly-known placeholder secret.**
   `appsettings.json` ships `Jwt:Secret = "REPLACE_VIA_ENV…change_me…"`, and nothing rejected it at startup. A prod
   environment that forgot to inject a real secret would boot happily and issue forgeable tokens.

Both are now **fail-loud, not fail-silent** (fixes below, independently re-verified).

---

## Claims Proven TRUE (independently re-verified)
- **Authentication**: register/login work; anonymous → **401** (not 302) + `WWW-Authenticate: Bearer`; tampered & **`alg=none`** tokens → 401.
- **Tenant isolation**: JWT claim authoritative; client `X-Tenant-Id` ignored without a valid gateway proof; disagreement throws. Verified **from source** (`HttpTenantProvider`) + live. 1,680 RLS predicates, fail-closed.
- **Security headers**: 9/9 OWASP headers + `x-correlation-id` present live.
- **Rate limiting**: auth endpoints throttle per-IP (`401×6 → 429×6`).
- **Error sanitization** (non-Dev): malformed input → RFC7807 with **no** exception/stack leak (verified in Local).
- **Mass-assignment resistance**: injected `tenantId/id/isAdmin/salary` ignored; only declared fields bind.
- **Health model**: liveness stays 200 through Redis/RabbitMQ/SQL outages (no restart storms); readiness → 503; clean recovery.
- **Provisioning idempotency**: repeat run exit 0, object counts stable.
- **Build**: solution builds **0 warnings / 0 errors** (`-warnaserror`); locked NuGet restore.
- **Tests that exist are real**: isolation/Identity/Core integration tests run against **real SQL** (Testcontainers/live); 0 tautologies; ~1.58 assertions/test. Architecture rules enforced via NetArchTest (**7/7 without coverage**).
- **Greenfield (as-remediated)**: `down -v` → one command → operational, no manual SQL.

## Claims Proven FALSE / overstated
- ❌ **"Greenfield bootstrap PASS" (prior closure)** — false as-documented; required a hidden env var (LocalDB crash otherwise).
- ❌ **"Architecture governance 7/7" (prior closure)** — true only *without* coverage; under the **CI coverage command it fails 3/7** (Coverlet `Tracker` types → `TypeLoadException`).
- ❌ **README.md "Day 1 scaffold… no business domain yet"** — materially stale; 16+ bounded contexts exist.
- ⚠️ **"Error handling sanitized"** — true in Local/Production; **Development leaks by design** (DeveloperExceptionPage) — acceptable but must not be the prod env.

## Undocumented steps (found)
- `export ConnectionStrings__KaramchariDb=…` was *required* yet absent from onboarding docs. **Eliminated** at root (fixed Dev connection string + `setup-local` exports it).

## Hidden dependencies (found — see `environment-dependencies.md`)
Connection-string env var (fixed); emulated `linux/amd64` SQL on Apple Silicon; HTTPS dev-cert trust; launchSettings forcing `Development`+port 60462 (overrides shell env; `:5000` without launch profile); macOS bash 3.2 / no `timeout` / no `pwsh`; empty `TrustedGatewayFingerprint` disables S2S header tenancy.

## Broken / stale documentation
- `docs/seed/local-dev-seed.sql` header (sqllocaldb, manual schema, "until provisioning exists") — **Incorrect**.
- README port story (8080) vs README-LOCAL (60462) vs reality (5000/60462/8080) — **conflicting**.
- Three setup narratives + two compose files — **doc sprawl**.

## Weak tests
- 85% of tests (556/657) are tenant-isolation; **10+ bounded contexts have no tests** (Billing, Compensation, Recruitment, Performance, Governance, Forecasting, Workflow, FinancialOps, Intelligence, Notifications). Mutation testing not yet run.

## Bootstrap failures
- One, critical, **fixed**: LocalDB `PlatformNotSupportedException` on non-Windows. Post-fix: deterministic zero-to-operational.

## Developer friction points
- As-found non-Windows onboarding was a hard fail; PowerShell-only official scripts; port/cert/sql-emulation gotchas. As-remediated: `< 5 min` to first authenticated call via `setup-local.sh`.

## Security risks
- **Closed**: placeholder-secret silent boot (now fail-fast in non-Dev/Local). 
- **Residual (low)**: TenantResolutionException → 500 in Development only; `/health` hang on dependency loss; intra-tenant BOLA not exhaustively fuzzed.

## Operational risks (weakest dimension)
- **No rollback, no DR (RTO/RPO), no executable deploy** (pipeline steps commented), **no on-call/incident docs**, single runbook. Telemetry/health/durability are strong; the operational paperwork and delivery automation are not.

## Technical debt
- 12+ EF value-comparer warnings; transactional bus-outbox disabled (MT 8.3.0); no committed Staging/Production appsettings; CI coverage⨉arch interaction; perf/soak under authenticated load unmeasured.

---

## Remediations applied during this audit (all build 0/0, re-verified)
| Change | File | Effect |
|---|---|---|
| Dev connection string → Docker SQL | `appsettings.Development.json` | Fresh-machine bootstrap works cross-platform; no hidden env var |
| **JWT production fail-fast guard** | `Program.cs` | Refuses to start in non-Dev/Local with missing/placeholder/<32-byte secret (verified: exit 134, 0 listeners) |
| One-command setup | `setup-local.sh` (+`.ps1`) | Zero-state → operational; self-sufficient connection wiring; report |
| One-command tests | `run-all-tests.sh` (+`.ps1`) | TRX+coverage+summary; correct pass/fail propagation; arch-without-coverage |
| 13 phase evidence docs | `docs/hostile-audit/*.md` | This audit trail |

Auditor self-corrected **4 bugs in own deliverables** during certification (verify-DB target, grep/SIGPIPE false-fail, bash word-split, bash-3.2 empty-array) — documented in the cert files.

---

## Final Scores (0–10)
| Dimension | Score | Basis |
|---|---:|---|
| Build Reproducibility | **7.5** | 0/0 deterministic, locked restore, reproducible greenfield *after fix*; CI likely red under coverage; was broken as-found |
| Developer Experience | **7.0** | `<5 min` one-command *after fix*; as-found non-Windows = fail; doc/script sprawl |
| Test Quality | **6.5** | Real-DB, meaningful, 0 tautologies; but lopsided coverage, untested contexts, coverage⨉arch |
| Documentation Quality | **5.5** | Partially accurate; stale README/seed; conflicting ports; missing conn step |
| Operational Readiness | **4.0** | No rollback/DR/deploy/on-call; strong telemetry/health/durability |
| Security Confidence | **8.5** | Authz, JWT (incl. alg-none), isolation (source-verified), headers, rate-limit, mass-assignment, fail-fast secret |
| Production Confidence | **5.5** | Core sound; gated by ops, deploy automation, prod config, unmeasured perf |
| Enterprise Readiness | **6.0** | Safe foundation for multi-team build-on *with conditions* |

**Composite: ~6.3 / 10**

---

## FINAL VERDICT: **CONDITIONALLY CERTIFIED**

The platform's **core is genuinely sound** — multi-tenant isolation, authentication/JWT integrity, security headers, rate limiting, health semantics, durability, and (after fix) a true one-command greenfield bootstrap all hold up under independent hostile testing, with several prior-report claims confirmed by source and live evidence.

But it would **NOT have passed as-found**: the documented bootstrap silently demanded a hidden env var, a forgotten production secret would silently enable token forgery, and the "7/7 architecture" claim is false under the actual CI command. These are exactly the silent-failure modes the audit targets. They are now closed or clearly flagged.

**Certification is therefore CONDITIONAL on:**
1. **Operational readiness** — author rollback, DR (RTO/RPO + backup/restore), an executable deploy pipeline, and on-call/incident runbooks. *(Highest priority; currently the weakest dimension.)*
2. **CI correctness** — run architecture tests without coverage (or exclude the assembly from instrumentation) so the pipeline is genuinely green; confirm integration-suite infra in CI.
3. **Production configuration** — commit a `appsettings.Production.json` baseline and startup validation for required connection strings (the Jwt guard is done).
4. **Documentation truth** — fix README status/ports, the stale seed header, and converge on `setup-local` as the single onboarding path.
5. **Test breadth** — add coverage for the untested bounded contexts; run mutation testing on Core/Payroll/Isolation.
6. **Performance** — a Release-build authenticated load/soak run (p95/p99, memory).

Until (1)–(2) are addressed, **do not let multiple teams build on top of it in production**. The foundation is trustworthy; the operational and delivery scaffolding is not yet.

*Assume nothing. Trust nothing. Verified everything that could be verified on this host; explicitly flagged what could not (PowerShell execution, mutation testing, full disk/DNS/network failure injection, authenticated load).*
