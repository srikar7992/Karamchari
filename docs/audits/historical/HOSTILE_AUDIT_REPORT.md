# Karamchari Hostile Audit Report

Audit date: 2026-05-30 IST.

Final verdict: NOT CERTIFIED.

## Claims Proven True

| Claim | Evidence |
|---|---|
| Backend builds from current workspace | `dotnet build src/Backend/Karamchari.sln -c Debug --no-restore --disable-build-servers` succeeded outside sandbox with 0 warnings and 0 errors. |
| Local greenfield dependency/database bootstrap can succeed | `./setup-local.sh --fresh --no-run` destroyed Docker volumes, started infrastructure, restored/built, provisioned DB/RLS/tenants, verified identity tables/RLS/signing key, and seeded data. |
| Full backend test runner can pass with infrastructure up | `./run-all-tests.sh` passed 10 test projects and produced TRX/Cobertura artifacts. |
| Health checks detect major dependency outages | Redis, RabbitMQ, and SQL container stops caused `/health` to return HTTP 503. SQL restart recovered to HTTP 200 after about 30s. |
| Basic authentication flow works | Register returned HTTP 200, login returned HTTP 200 with tokens, authenticated `GET /api/v1/hr/employees` returned HTTP 200. |
| Unauthenticated protected endpoints are blocked | Logout and HR employee list without JWT returned HTTP 401. |
| Security headers exist | `/health` response included CSP, HSTS, frame denial, no-sniff, referrer policy, permissions policy, COOP/CORP/COEP, correlation id, and traceparent. |

## Claims Proven False or Not Proven

| Claim | Result | Evidence |
|---|---|---|
| `setup-local` leaves system operational and requires nothing else | NOT PROVEN / FAIL in this harness | The script reached health 200 during execution, but API was not reachable after the command returned. A separate live API session was required for later checks. |
| Documentation is accurate | FALSE | `README.md` still says Day 1 scaffold/no business domain and points to `./setup.ps1`; repo has many modules and verified script was `setup-local.sh`. |
| Test runner satisfies all required outputs | FALSE | TRX and coverage XML exist; no JUnit or HTML report; no mutation testing. |
| Production deployment is certified | FALSE | `deploy-api.yml` has commented Azure/login/deploy steps and echo validation. |
| Operational readiness is certified | FALSE | Runbooks lack production commands, on-call contacts, rollback procedures, restore procedures, alert routes, and DR evidence. |
| Security is fully certified | NOT PROVEN | Basic auth/header checks passed, but live tenant/object authorization, privilege escalation, and JWT rotation were not exhaustively tested. |

## Undocumented Steps

- Host-level execution was required for Docker/curl/MSBuild. Sandbox execution could not access Docker and hung/faulted MSBuild.
- Docker Desktop on Apple Silicon ran SQL Server under `linux/amd64` emulation.
- ASP.NET DataProtection uses `/Users/srikarbojji/.aspnet/DataProtection-Keys`; setup does not document or validate this.
- API persistence after setup command is not proven; live API checks required starting the API in a dedicated session.

## Hidden Dependencies

- Docker Desktop daemon.
- .NET SDK 10.0.300 or compatible.
- `curl`, `nc`, Docker Compose plugin.
- Local ports: 1433, 6379, 5672, 15672, 8081, 9090, 3000, 8025, 60462, 60463.
- SQL Server container `sqlcmd` at `/opt/mssql-tools18/bin/sqlcmd`.
- Local dev SQL password `Karamchari@123`.
- RabbitMQ `guest/guest`.
- Host user profile for DataProtection keys.
- Existing `node_modules` and untracked workspace files may mask clean-room issues.

## Broken Documentation

- `README.md` is stale relative to current repository structure.
- `README.md` references `./setup.ps1`, not the verified one-command local script.
- Dashboard docs mostly match local services, but `README-LOCAL.md` startup command omits Prometheus/Grafana while ports are documented.
- No doc warns about Apple Silicon SQL Server emulation.

## Weak Tests

- No Stryker/mutation testing configuration found.
- No HTML coverage report generated.
- No JUnit output generated.
- Some security and chaos tests are modeled/synthetic rather than live exploit/failure traffic.
- Frontend/mobile tests are not included in `run-all-tests.sh`.

## Bootstrap Failures

- Non-escalated setup failed at Docker daemon access.
- API runtime after `setup-local.sh` command return was not observable in this harness even after script hardening attempts.

## Developer Friction Points

- Multiple setup scripts/docs exist with inconsistent names.
- Dirty/untracked workspace makes it hard to distinguish official setup from local remediation.
- Docker and MSBuild behave differently inside vs outside sandbox.
- The developer must understand when to use `setup-local.sh`, `verify-local.sh`, direct API run, or Docker Compose.

## Security Risks

- Development secrets are committed for SQL and RabbitMQ.
- Production config files are absent; production secret contract is incomplete, although JWT placeholder guard exists.
- Tenant/object authorization was only partially live-tested.
- JWT rotation and signing-key operational procedure are not certified.

## Operational Risks

- Deployment pipeline is not real.
- Rollback is not automated.
- DR/restore is not automated or documented with commands.
- Grafana dashboards, Prometheus alerts, Alertmanager routes, SLO/error budget docs, and escalation policies are missing.
- CODEOWNERS/bus factor remains weak from prior evidence.

## Technical Debt

- EF warnings for collection properties using value converters without value comparers appeared during health checks.
- Docker Compose file emits an obsolete `version` warning.
- Architecture tests require a coverage exception.
- API startup persistence through setup script remains unproven in this harness.

## Final Scores

| Category | Score | Rationale |
|---|---:|---|
| Build Reproducibility | 75/100 | Backend builds cleanly outside sandbox; clean-room NuGet/cache deletion not performed. |
| Developer Experience | 55/100 | Setup/provisioning works, but docs are inconsistent and API persistence after setup is not proven. |
| Test Quality | 60/100 | Full backend suite passes and has meaningful tests; mutation/JUnit/HTML/risk thresholds missing. |
| Documentation Quality | 45/100 | Some local docs accurate; README stale and operational docs incomplete. |
| Operational Readiness | 25/100 | Health checks work; production recovery/deploy/rollback/DR missing. |
| Security Confidence | 60/100 | Basic auth, headers, rate-limit code, and tests exist; live tenant/object abuse testing incomplete. |
| Production Confidence | 20/100 | CD is fake/commented; production config and rollback missing. |
| Enterprise Readiness | 35/100 | Strong codebase foundations, but independent operations and production certification fail. |

## Final Verdict

NOT CERTIFIED.

The platform is locally buildable, testable, provisionable, and partially failure-detecting. It is not certified as independently operable or production-ready because deployment, rollback, persistent one-command runtime, operations, mutation testing, and complete documentation truthfulness are not proven.
