# Local Setup and Validation

## One-command setup

Windows:

```powershell
.\setup.ps1
```

Linux/WSL:

```bash
git config core.hooksPath .githooks
./scripts/validate.sh
```

## What setup enforces

- Git hooks are activated through `.githooks`.
- The .NET SDK is pinned by `global.json`.
- NuGet restore uses committed `packages.lock.json` files.
- Backend builds run with analyzers and warnings as errors.
- Frontend and mobile dependencies install through `npm ci` when Node.js is available.

## Full local gate

Run:

```powershell
.\scripts\validate.ps1
```

This is the same contract as CI: locked restore, formatting, build/analyzers, tests with coverage, NuGet audit, npm audits, and API publish validation.

The SQL Server RLS integration tests use Testcontainers and require Docker. Use `.\scripts\validate.ps1 -SkipIntegration` only for fast local feedback; it is not a merge gate.

## Secret handling

Do not commit environment-specific `appsettings.*.json`, `.env`, certificates, keys, or connection strings. Use environment variables, `dotnet user-secrets`, or managed cloud secret stores.
