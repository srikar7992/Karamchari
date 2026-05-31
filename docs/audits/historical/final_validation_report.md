# Engineering Hardening Validation Report

## Gates now enforced

- SDK pinning: `global.json` pins .NET SDK `10.0.203`.
- Package determinism: `Directory.Build.props` enables package lock files and CI locked restore.
- Package source governance: `NuGet.config` clears ambient package sources and allows only `nuget.org`.
- Build determinism: deterministic compilation and CI build metadata are enabled globally.
- Zero warnings: `TreatWarningsAsErrors`, .NET analyzers, code style enforcement, and `dotnet build -warnaserror` are required.
- Formatting: `.editorconfig` is enforced by `dotnet format --verify-no-changes`.
- Local enforcement: `.githooks/pre-commit` and `.githooks/pre-push` gate formatting, build, tests, audits, and staged secret patterns.
- CI enforcement: `.github/workflows/ci.yml` runs backend restore, build, tests, coverage, audit, publish validation, npm audits, portal typecheck/build, artifact retention, and secret scanning.
- Branch readiness: PR template and CODEOWNERS are present for required reviews and status checks.

## Issues fixed during this hardening pass

- Generated and committed NuGet `packages.lock.json` files for deterministic restore.
- Replaced broad warning suppression with a documented `CA1848` policy exception while generated logging delegates are adopted.
- Fixed TDS calculation when no HRA declarations exist by using an empty-safe maximum.
- Fixed EPF ECR name normalization so special characters become single spaces instead of collapsing words.
- Corrected payroll tests for old-regime 87A rebate behavior and critical risk level after 30 days.
- Completed Identity integration test DbContext replacement for Capability, Governance, Intelligence, and Recruitment contexts.
- Added a deterministic TimeAttendance test so the project is discoverable by the test gate.
- Added npm `postcss` overrides and regenerated frontend/mobile lock files to clear the moderate PostCSS advisory.
- Scoped deterministic `npm ci` logging to errors because Expo/React Native still emit upstream transitive deprecation notices for install-time tooling; dependency risk remains enforced by `npm audit --audit-level=moderate`.

## Validation commands

```powershell
dotnet restore src/Backend/Karamchari.sln --locked-mode
dotnet format src/Backend/Karamchari.sln --verify-no-changes --severity warn --no-restore
dotnet build src/Backend/Karamchari.sln --no-restore -c Release -warnaserror
dotnet test src/Backend/Karamchari.sln --no-build -c Release --logger "trx;LogFilePrefix=test_results" --collect:"XPlat Code Coverage"
dotnet list src/Backend/Karamchari.sln package --vulnerable --include-transitive
npm audit --prefix src/Frontend/portal --audit-level=moderate
npm run --prefix src/Frontend/portal typecheck
npm run --prefix src/Frontend/portal build
npm audit --prefix src/Mobile/karamchari-mobile --audit-level=moderate
dotnet publish src/Backend/Karamchari.Api/Karamchari.Api.csproj --no-restore -c Release -o artifacts/publish/Karamchari.Api -warnaserror
rg -n "TODO: Add XML documentation|Your_password123|AccountKey=|SharedAccessKey=" -g "!artifacts/**" -g "!.git/**" -g "!docs/**" -g "!**/package-lock.json"
```

## Remaining operational dependency

The SQL Server RLS integration tests require Docker because they intentionally validate SQL Server row-level security through Testcontainers. CI runs on GitHub-hosted Linux runners where Docker is available. Local machines must run Docker before executing the full validation gate.
