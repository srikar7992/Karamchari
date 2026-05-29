# Environment Dependency Discovery

## Verified Dependencies

| Dependency | Evidence |
|---|---|
| Docker Desktop / Docker daemon | `docker info` required outside sandbox; Docker Desktop 29.5.2 verified. |
| Docker Compose | `docker compose` used for local stack. |
| .NET SDK 10 | `setup-local.sh` detected `.NET SDK: 10.0.300`. |
| curl | Required for health checks. |
| SQL Server tools in container | `sqlcmd` used from SQL container path `/opt/mssql-tools18/bin/sqlcmd`. |
| Local ports | 1433, 6379, 5672, 15672, 8081, 9090, 3000, 8025, 60462, 60463. |
| ASP.NET DataProtection key path | API log: `/Users/srikarbojji/.aspnet/DataProtection-Keys`; keys not encrypted at rest. |
| ASP.NET dev certificate | HTTPS health works only if local certificate setup is usable; script does not validate it explicitly. |
| User profile state | API uses host user profile for DataProtection. |
| Apple Silicon emulation | SQL image warning: requested `linux/amd64` on host `linux/arm64/v8`. |
| Default local secrets | SQL password `Karamchari@123`, RabbitMQ `guest/guest`, JWT dev placeholder in config. |
| Node/npm state | Frontend `node_modules` exists in workspace and can mask fresh install issues. |
| Existing untracked files | Worktree contains many untracked docs, migrations, scripts, and code files. |

## Hidden or Underdocumented Dependencies

- Docker daemon access differs between sandbox and host.
- Setup relies on local Docker Desktop, not just Docker CLI presence.
- Development uses host profile DataProtection keys.
- SQL Server image platform mismatch is not documented.
- The local setup script uses `docs/seed/local-dev-seed.sql`, but seed idempotency is not proven by tests.

## Verdict

Environment reproducibility is improved but not clean-room certified.
