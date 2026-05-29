# Developer Experience Audit

## Measured Events

| Event | Evidence |
|---|---|
| Time to backend build | `dotnet build --no-restore --disable-build-servers` outside sandbox completed in about 3.25s after dependencies were present. |
| Time to greenfield bootstrap | `./setup-local.sh --fresh --no-run` completed successfully; exact wall time not captured in report, but execution completed during this audit session. |
| Time to first login | Register and login through live API succeeded after setup and live API start. |
| Time to first API call | Authenticated `GET /api/v1/hr/employees` returned HTTP 200. |
| Time to first workflow | Not measured. |

## Friction Points

- `README.md` points to `setup.ps1`, not the verified `setup-local.sh`.
- The API process from `setup-local.sh` was not observable after script exit in this execution harness.
- Docker access required host-level execution, not sandbox execution.
- SQL Server image runs under amd64 emulation on Apple Silicon.
- No independent unfamiliar human performed the flow, so this is an agent-run audit, not full DX certification.

## Verdict

Partially certified. Under-30-minute target is plausible for this machine, but not independently validated with an unfamiliar developer.
