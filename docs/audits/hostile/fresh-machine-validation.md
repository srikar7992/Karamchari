# Fresh Machine Validation

Audit date: 2026-05-30 IST.

## Method

Used the repository's one-command setup path from a destructive local Docker reset:

```bash
./setup-local.sh --fresh --no-run
```

This executes `docker compose down -v`, starts local infrastructure, restores/builds the solution, provisions database/RLS/dev tenants, verifies database state, and applies `docs/seed/local-dev-seed.sql`.

## Evidence

First non-escalated execution failed because Docker daemon access was unavailable in the sandbox:

```text
[FAIL] docker daemon not running
[FAIL] Prerequisite gate failed -- aborting.
```

Docker was then verified outside the sandbox:

```text
Docker version 29.5.2
Server Version: 29.5.2
Containers: 9 Running: 9
```

Fresh setup outside sandbox succeeded:

```text
[ OK ] volumes destroyed
[ OK ] compose up issued
[ OK ] SQL Server accepting connections
[ OK ] restore
[ OK ] build (0 errors)
[ OK ] provisioning exit 0
[ OK ] identity tables = 11
[ OK ] tenant schemas = 3
[ OK ] RLS policies = 3
[ OK ] signing keys = 1
[ OK ] seed applied
SETUP SUCCEEDED -- system operational from zero state.
```

## Findings

| Check | Result | Evidence |
|---|---|---|
| No prior Docker volumes | PASS | `docker compose down -v` removed `local_sqlserver-data`, `local_redis-data`, `local_rabbitmq-data`, `local_seq-data`, `local_prometheus-data`, and `local_grafana-data`. |
| Restore/build from repo | PASS | `dotnet restore` and `dotnet build` succeeded inside setup. |
| Database bootstrap | PASS | Provisioning returned exit 0. |
| RLS bootstrap | PASS | Setup verified 3 RLS policies. |
| Tenant bootstrap | PASS | Setup verified 3 tenant schemas. |
| Seed bootstrap | PASS | Setup applied `docs/seed/local-dev-seed.sql`. |
| CPU architecture portability | PARTIAL | SQL Server image emitted `linux/amd64` on `linux/arm64/v8`; it ran under Docker Desktop emulation, but this is an undocumented host-platform dependency. |

## Verdict

Conditionally successful for local infrastructure and database bootstrap. Not fully certified as a brand-new-machine claim because the validation used the current local repo with existing untracked files and did not remove NuGet, npm, IDE, ASP.NET dev cert, or user profile state.
