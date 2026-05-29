# Documentation Truthfulness Audit

## Documents Audited

| Document | Status | Evidence |
|---|---|---|
| `README.md` | Partially Accurate | Mentions `./setup.ps1`, `./scripts/runtime/run-local.ps1`, and `./scripts/validation/validate-runtime.ps1`. The verified working path was `./setup-local.sh`; `README.md` also claims "Day 1 scaffold" and only HR/Payroll layout while the repo now has many modules. |
| `README-LOCAL.md` | Partially Accurate | Docker stack, build, provision, seed, and health commands match actual flow. It omits Prometheus/Grafana in the startup command even though ports are documented and `setup-local.sh` starts them. |
| `docs/engineering/local_setup.md` | Not re-executed | Not enough time to execute every command in every engineering doc. Must not inherit prior certification. |
| `docs/certification/*` | Not trusted | Prior certification docs exist, but this audit did not inherit PASS from them. |
| `docs/hostile-audit/*` prior files | Superseded | Prior hostile-audit files existed before this pass and were replaced/updated where relevant. |

## Documented URL Checks

With infrastructure up:

| URL | Result |
|---|---|
| `http://localhost:8081` Seq | HTTP 200 |
| `http://localhost:15672` RabbitMQ | HTTP 200 |
| `http://localhost:3000` Grafana | HTTP 302 |
| `http://localhost:9090` Prometheus | HTTP 302 |
| `http://localhost:8025` Mailpit | HTTP 200 |

With API running in a live audit session:

| URL | Result |
|---|---|
| `https://localhost:60462/health` | HTTP 200 |
| `https://localhost:60462/scalar` | Not rechecked successfully after setup script exit; API process was not persistent in this execution harness. |

## Broken or Risky Claims

- `README.md` claims `./setup.ps1` is the one-command setup, but the requested and verified scripts are `setup-local.sh` / `setup-local.ps1`.
- `README.md` says "Day 1 scaffold. Foundations only -- no business domain yet", which is outdated given Payroll, TimeAttendance, PSA, Performance, Billing, Forecasting, etc.
- Documentation does not warn that Docker Desktop on Apple Silicon runs the SQL Server image under `linux/amd64` emulation.
- Documentation does not explain the sandbox/host distinction needed for Docker, curl, and MSBuild in this environment.

## Verdict

Documentation Quality: Partially Accurate.
