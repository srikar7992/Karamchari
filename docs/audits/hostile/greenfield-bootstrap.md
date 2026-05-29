# Greenfield Bootstrap Re-Certification

## Destructive Reset Executed

```bash
./setup-local.sh --fresh --no-run
```

This ran `docker compose down -v` and removed local Docker volumes for SQL Server, Redis, RabbitMQ, Seq, Prometheus, and Grafana.

## Bootstrap Result

PASS for infrastructure and database bootstrap:

- Containers started.
- SQL accepted connections.
- Restore/build succeeded.
- Provisioning succeeded.
- Identity tables = 11.
- Tenant schemas = 3.
- RLS policies = 3.
- Signing keys = 1.
- Seed applied.

## Runtime Result

PARTIAL/FAIL for persistent API runtime through one command:

- `./setup-local.sh` reached `/health = 200` during script execution.
- After the script exited, the API process could not be observed and `curl https://localhost:60462/health` returned connection failure in this harness.
- A live API session launched separately stayed up and served `/health = 200`.

## Verdict

Greenfield bootstrap of dependencies and database is certified in this environment. Greenfield "system operational after one command" is not certified.
