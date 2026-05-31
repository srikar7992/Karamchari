# Phase 2 — Documentation Truthfulness Audit

Every documented command/URL/port/env-var was executed or checked against the running system.
Verdict per document: **Accurate / Partially Accurate / Incorrect**.

## Per-document verdict

| Document | Verdict | Evidence |
|---|---|---|
| `README.md` | **Partially Accurate** | Status line "Day 1 scaffold. Foundations only — no business domain yet." is **stale** (16+ bounded contexts exist: HR, Payroll, Billing, Compensation, Recruitment, etc.). Lists API at `http://localhost:8080` (only true for the *containerized* compose), while the documented `dotnet run` dev workflow listens on `60462/60463` (launchSettings) — conflicting port story vs README-LOCAL. Referenced scripts (`setup.ps1`, `scripts/runtime/run-local.ps1`, `scripts/validation/validate-runtime.ps1`, `scripts/chaos/run-chaos.ps1`) all **exist**. |
| `README-LOCAL.md` | **Partially Accurate** | Commands run, BUT the critical provisioning step omits the connection-string requirement → on a fresh non-Windows machine provisioning **crashes** (`PlatformNotSupportedException: LocalDB`). See `fresh-machine-validation.md`. The `dotnet run --project ... --provision-dev-tenants` form (no `--` separator) was tested and **does** forward the arg in .NET 10 (verified empirically) — so that part is fine. Ports/URLs (1433/6379/5672/15672/8081/3000/9090/8025, Scalar at `/scalar`) verified correct. |
| `docs/seed/local-dev-seed.sql` (header) | **Incorrect / dangerously stale** | Header instructs `sqllocaldb start MSSQLLocalDB`, manual `CREATE SCHEMA [tenant_dev]`, and says *"Until the Provisioning service exists, create the tenant schema manually"* — contradicting the entire current automated provisioning model. A developer following it would diverge badly. |
| `docs/certification/startup.md` | **Accurate (but mislocated)** | This is the **only** onboarding-relevant doc that shows `export ConnectionStrings__KaramchariDb='Server=localhost,1433;...'` — the step that actually makes provisioning work. It lives in a certification artifact, not the quick-start, so new devs never see it. |
| `docs/engineering/local_setup.md` | **Redundant** | A *third* setup narrative alongside README + README-LOCAL. Doc sprawl; risks divergence. |
| `infrastructure/local/docker-compose.yml` | **Accurate** | `docker compose up -d` brought up all 9 services; ports match README-LOCAL. (Emits a cosmetic warning: obsolete `version:` attribute.) |
| Root `docker-compose.yml` | **Partially Accurate / confusing** | A *second* compose for containerized API+Worker (port 8080, `ASPNETCORE_ENVIRONMENT=Development`, mandatory `KARAMCHARI_SQL_PASSWORD`). Diverges from `infrastructure/local` (redis has no volume/appendonly here). Two compose files with different topologies is a footgun. |

## Documented surfaces verified live
- `GET /health` → 200; `GET /health/live` → 200; `GET /health/ready` → 200 (and 503 under dependency loss — see `failure-injection.md`).
- Scalar served at `/scalar` (302 redirect from `/`) in Development and Local.
- Seq `:8081`, RabbitMQ mgmt `:15672`, Grafana `:3000`, Prometheus `:9090`, Mailpit `:8025` — all reachable.
- Provisioning arg form `dotnet run ... -- --provision-dev-tenants` **and** `... --provision-dev-tenants` both forward correctly under .NET 10 (tested in an isolated probe).

## Key truthfulness findings
1. **README.md "Day 1 scaffold / no business domain"** — materially false; undersells (and misdescribes) a large platform.
2. **Conflicting ports** across README (8080) vs README-LOCAL (60462) vs reality (launchSettings 60462, Kestrel default 5000 without launch profile, 8080 in container).
3. **Missing connection-string step** in onboarding → fresh-machine bootstrap failure (the headline defect).
4. **`local-dev-seed.sql` header** describes a provisioning model that no longer exists.

## Verdict
Documentation = **Partially Accurate overall**, with one *Incorrect* (seed header) and one onboarding omission severe enough to break a fresh machine. Remediation (this audit) fixed the connection-string root cause; doc text corrections are recommended (see HOSTILE_AUDIT_REPORT.md).
