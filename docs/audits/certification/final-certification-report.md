# Karamchari Platform — Final Certification Report

**Date:** 2026-05-29
**Scope:** End-to-end certification of every implemented capability against the *running, provisioned* application.
**Environment:** macOS arm64 · .NET 10.0.300 · Docker 29.5.2 · SQL Server 2022, Redis 7.4, RabbitMQ 3.13 (live).
**Build:** `Karamchari.sln` — **succeeded, 0 warnings, 0 errors.**
**Tests:** `dotnet test Karamchari.sln` — **714 passed / 0 failed** (9 projects) + ArchitectureTests **7/7** (run separately).

> **Headline verdict: NOT PRODUCTION READY.** The platform has an exceptionally strong multi-tenant data-isolation core (schema-per-tenant + 1,680 RLS predicates, 610 passing isolation tests) and clean architecture, but **authentication is non-functional in a fresh deployment**, which cascades to block every authenticated business capability. Several security-hardening and messaging-isolation gaps compound the risk.

---

## 1. PASS / FAIL by category

| # | Category | Verdict | Evidence |
|---|---|---|---|
| 1 | Environment | ✅ **PASS** | `environment.md` |
| 2 | Application Startup | ✅ **PASS** (warnings) | `startup.md` |
| 3 | Scalar / OpenAPI | ✅ **PASS** | `scalar.md` |
| 4 | Authentication | ❌ **FAIL (CRITICAL)** | `authentication.md` |
| 5 | Employee Domain | ⛔ **BLOCKED** (logic ✅ via tests) | `employee.md` |
| 6 | Multi-Tenant Isolation | ✅ **PASS** (DB/cache) / ⚠️ **bus gap** | `tenant-isolation.md` |
| 7 | Redis | ✅ **PASS** (design+wiring) | `redis.md` |
| 8 | RabbitMQ | ⚠️ **PARTIAL** | `rabbitmq.md` |
| 9 | Outbox | ⚠️ **PARTIAL** | `outbox.md` |
| 10 | Database | ✅ **PASS** (findings) | `database.md` |
| 11 | OpenTelemetry | ⚠️ **PARTIAL** | `opentelemetry.md` |
| 12 | Logging | ✅ **PASS** (visibility gap) | `logging.md` |
| 13 | Correlation | ⚠️ **PARTIAL** | `correlation.md` |
| 14 | Security | ❌ **FAIL** | `security.md` |
| 15 | Performance Smoke | ✅ **PASS** (health only) | `performance.md` |
| 16 | Chaos | ✅ **PASS** | `chaos.md` |
| 17 | Architecture | ✅ **PASS** (CI gap) | `architecture.md` |
| 18 | Business Workflows | ⛔ **BLOCKED** | `business-workflows.md` |

**Tally:** 9 PASS · 2 FAIL · 5 PARTIAL · 2 BLOCKED.

---

## 2. Production-Readiness Scorecard (0–10)

| Dimension | Score | Rationale |
|---|---:|---|
| **Infrastructure** | 8 | Clean compose stack, healthy deps, SQL persistence. −2: no durable volumes for redis/rabbit; collector/worker not part of default up. |
| **Architecture** | 8 | NetArch tests pass; clean modular monolith, schema-per-tenant, outbox. −2: arch tests excluded from solution/CI. |
| **Security** | 2 | Auth broken; 302-not-401; missing security headers; per-request `BuildServiceProvider`; verbose error leakage in `Local`. Strong RLS keeps it off 0. |
| **Observability** | 6 | Real W3C tracing, EF activities, rich Serilog enrichment, comprehensive health. −4: properties not rendered in text sinks, logs don't reach Seq, no collector asserted. |
| **Performance** | 5 | Health smoke 0-error & stable pool. Unproven: authenticated load, percentiles, soak, memory (debug build only). |
| **Reliability** | 6 | Survives Redis/RabbitMQ outage & recovers; outbox/retry/DLQ/circuit-breaker designed. −4: no liveness/readiness split; provisioning non-idempotent (SIGABRT). |
| **Developer Experience** | 6 | Build clean, one-command infra, Scalar, 721 tests. −4: documented `--provision-dev-tenants` exits 134 and can't be re-run; LocalDB default cs broken on macOS. |
| **Data Isolation** (bonus) | 9 | 1,680 RLS predicates, schema-per-tenant, cache guard, 610 isolation tests. −1: RabbitMQ tenant filters missing. |
| **Production Readiness (overall)** | **3.5 / 10** | Blocked by auth + security; excellent foundation otherwise. |

---

## 3. Findings by severity

### CRITICAL (2)
1. **Authentication non-functional in fresh env** — `IdentityDbContext` has no migrations and is omitted from `--provision-dev-tenants`; `identity.AspNetUsers` never created → register/login 500. Blocks Phases 5,6(http),8,9,13,14,18.
2. **Provisioning crashes / not idempotent** — `ProvisionRlsInfrastructureAsync` re-run throws SQL 3729; process exits **134 (SIGABRT)**. The documented bootstrap command is not safely repeatable.

### HIGH (5)
3. API returns **302 → /Account/Login instead of 401** for missing/invalid tokens (wrong default challenge scheme).
4. **`BuildServiceProvider()` inside `IssuerSigningKeyResolver`** — new DI container per token validation (memory/DoS).
5. **RabbitMQ transport omits tenant filters** (`TenantConsume/Publish/SendFilter`) that InMemory/ASB branches apply — tenant isolation not enforced on the bus.
6. **Missing security headers** (CSP, HSTS, X-Frame-Options, X-Content-Type-Options, Referrer-Policy).
7. **Verbose error leakage in `Local` env** — `UseDeveloperExceptionPage` enabled for `Development` **and** `Local`; leaks stack traces + request headers.

### MEDIUM (8)
8. ~30+ decimal columns without precision/scale → silent truncation in payroll/billing money math.
9. Only **41 FKs / 589 tables** — referential integrity largely not DB-enforced.
10. No liveness/readiness split — transient dep outage can kill live pods.
11. ArchitectureTests not in solution → not enforced in CI.
12. Prod `GlobalExceptionHandler` returns raw `exception.Message` to clients.
13. Logs not reaching Seq (sink points at `:4317` collector, not running); README advertises Seq.
14. Text log sinks omit `{Properties}` → enrichment invisible in console/file.
15. Rate limiter not observably enforcing on tested path.

### LOW (3)
16. No durable volumes for redis/rabbitmq in compose.
17. Correlation id not echoed to clients in a response header.
18. Collection value-converters lacking value comparers (EF change-tracking edge cases).

---

## 4. Top 20 Risks Before Production
1. No one can log in (auth broken) — total functional blocker.
2. Provisioning bootstrap crashes on re-run (SIGABRT) — fragile deploys/restarts.
3. Tenant isolation **not** enforced on RabbitMQ — cross-tenant message risk.
4. 401-as-302 breaks all SPA/mobile/API clients and masks auth failures.
5. Per-request `BuildServiceProvider` → memory blowup/DoS under auth load.
6. Decimal truncation in payroll/billing → financial correctness errors.
7. Missing security headers → clickjacking/MIME/transport downgrade exposure.
8. `Local` profile leaks stack traces + headers if ever exposed.
9. Sparse FKs → orphaned/inconsistent financial records.
10. No liveness/readiness split → orchestrator kills healthy pods on dep blips.
11. Async business flows unverified end-to-end (Worker + auth needed).
12. Outbox→consumer path never exercised live → unknown delivery behavior.
13. Architecture rules not in CI → silent boundary regressions.
14. Observability blind in prod (logs not reaching Seq; no collector asserted).
15. Identity signing keys are DB-resolved but identity store doesn't exist → key rotation untested.
16. Raw exception messages (SQL text) returned to clients in prod handler.
17. No authenticated load/perf/soak data → unknown scaling behavior.
18. Redis/RabbitMQ non-durable in provided compose → data loss on recreate.
19. No verified cross-tenant HTTP test (BOLA) due to auth block.
20. Rate limiting effectiveness unconfirmed → brute-force/DoS exposure on auth endpoints.

## 5. Top 20 Improvements Before Phase-1 Feature Expansion
1. Add EF migrations for `IdentityDbContext`; include it in `--provision-dev-tenants`.
2. Make `ProvisionRlsInfrastructureAsync` idempotent (drop policy before function; `IF EXISTS` guards) and return exit 0.
3. Pin default authenticate/challenge scheme to JWT Bearer; return 401 + `WWW-Authenticate`.
4. Replace `BuildServiceProvider()` in the signing-key resolver with injected/cached singleton resolver.
5. Apply `TenantConsume/Publish/SendFilter` + `ConfigureEndpoints` on the RabbitMQ branch (parity with InMemory/ASB).
6. Add a security-headers middleware (CSP, HSTS, X-CTO, X-Frame, Referrer-Policy).
7. Restrict `DeveloperExceptionPage` to `Development` only; sanitize prod ProblemDetails (no raw messages).
8. Set explicit precision/scale (`HasPrecision`) on all money/score decimals.
9. Add intra-aggregate FKs / review referential-integrity strategy.
10. Split `/health/live` and `/health/ready`.
11. Add `Karamchari.ArchitectureTests` to the solution and CI gate.
12. Point a Serilog sink at Seq (`:5341`) or run the collector with a Seq exporter; add `{Properties:j}` to text templates.
13. Stand up `otel-collector` + Prometheus by default; assert traces/metrics land.
14. Run + document the Worker so async flows are certifiable.
15. Provide a working local connection string (compose env / `appsettings.Local.json`) instead of LocalDB default.
16. Add durable volumes for redis/rabbitmq in local compose.
17. Echo correlation id (`X-Correlation-Id`/`traceparent`) on responses.
18. Add value comparers for converted collection properties.
19. Add authenticated load/soak tests with p50/p95/p99 + memory tracking on a Release build.
20. Verify rate-limiter policies on auth endpoints; add brute-force protection/lockout.

---

## 6. Method & Evidence
All evidence captured under `docs/certification/` and `docs/certification/evidence/` (build.log, provision.log, api-run.log, test-run.log, openapi.json, trx/). Every verdict above is backed by a real HTTP call, SQL query, container action, test run, or cited source file — no assumed results. Where a capability could not be exercised against the running app, it is marked **BLOCKED/PARTIAL** rather than PASS.

**Final statement:** The Karamchari platform is a well-architected, strongly tenant-isolated modular monolith with a broken front door. Fix the 2 CRITICAL and 5 HIGH findings (est. small, well-scoped changes) and the bulk of PARTIAL/BLOCKED phases become certifiable in a re-run.
