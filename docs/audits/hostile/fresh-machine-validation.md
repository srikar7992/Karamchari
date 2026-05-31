# Phase 1 — Fresh Machine Validation

**Auditor stance:** external; no prior PASS inherited. Host: macOS (arm64), .NET SDK 10.0.300, Docker 29.5.2.

## Method
A new developer follows the onboarding docs **verbatim**. The repo's own guides are the contract:
- `README.md` → "Run One-Command Setup: `./setup.ps1`" then `./scripts/runtime/run-local.ps1`.
- `README-LOCAL.md` → manual sequence: `docker compose ... up -d` → `dotnet restore/build` → `dotnet run --project src/Backend/Karamchari.Api --provision-dev-tenants` → seed → `dotnet run`.

No undocumented step is permitted. **If any undocumented step is required → FAIL.**

## Result (AS-FOUND): ❌ FAIL

Following the documented workflow on a clean machine, **database provisioning crashes**:

```
[ERR] Provisioning failed.
System.PlatformNotSupportedException: LocalDB is not supported on this platform.
   at Microsoft.Data.SqlClient.ManagedSni.LocalDB.GetLocalDBConnectionString(...)
   ...
   at Program.<Main>$(String[] args) in .../Program.cs:line 145
EXIT=1
```

### Root cause
`src/Backend/Karamchari.Api/appsettings.Development.json` pinned the connection string to a
**Windows-only** provider:

```
"KaramchariDb": "Server=(localdb)\\MSSQLLocalDB;Database=Karamchari_Local;Trusted_Connection=True;..."
```

The documented local workflow uses **Docker SQL Server** (`localhost,1433`, `sa`/`Karamchari@123`), not LocalDB.
On macOS/Linux LocalDB does not exist, so `Database.MigrateAsync()` throws before any object is created.

### The hidden step
The only places the correct connection is supplied are:
- `docs/certification/startup.md` (a *certification artifact*, not onboarding) — `export ConnectionStrings__KaramchariDb='Server=localhost,1433;...'`
- `infrastructure/local/docker-compose.yml` — for the **containerized** API (`Server=sqlserver`).

Neither `README.md` nor `README-LOCAL.md` instructs the developer to set `ConnectionStrings__KaramchariDb`.
The prior "greenfield PASS" was therefore only reachable with an **undocumented environment variable**
in the operator's shell. The local user-secrets store (`~/.microsoft/usersecrets/karamchari-api-dev/`)
was confirmed **empty** — so nothing on a fresh machine masks the defect.

**Per the Phase-1 rule (undocumented step required → FAIL), the as-found fresh-machine path FAILS.**

## Remediation applied (this audit)
1. `appsettings.Development.json` → `KaramchariDb` now points at Docker SQL (`Server=localhost,1433;...`),
   the canonical local DB per `README-LOCAL.md`. Cross-platform; no env var needed.
2. `setup-local.sh` / `setup-local.ps1` (new) export `ConnectionStrings__KaramchariDb/Redis/RabbitMQ`
   defensively so the one-command path is self-sufficient regardless of appsettings drift.

## Result (AS-REMEDIATED): ✅ PASS — independently re-verified
Bare documented workflow, **no environment override** (`ConnectionStrings__KaramchariDb` unset):

```
$ ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/Backend/Karamchari.Api -- --provision-dev-tenants
[INF] Provisioning complete.   EXIT=0
```
Post-state (queried against the Karamchari database):
```
identity_tables = 11   tenant_schemas = 3   rls_policies = 3   rls_predicates = 1680   signing_keys = 1
```

## Verdict
- **AS-FOUND: FAIL** (documented fresh-machine bootstrap crashes; required an undocumented env var).
- **AS-REMEDIATED: PASS** (root cause fixed; bare documented command now provisions cleanly cross-platform).
