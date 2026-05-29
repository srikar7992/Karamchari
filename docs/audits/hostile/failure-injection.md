# Failure Injection Audit

API was run in a live audit session with local infrastructure up.

## Baseline

`GET https://localhost:60462/health` returned HTTP 200 with `status: Healthy`.

## Injected Failures

| Failure | Command | Observed Behavior | Recovery |
|---|---|---|---|
| Redis down | `docker compose -f infrastructure/local/docker-compose.yml stop redis` | `/health` returned HTTP 503, `status: Unhealthy`; DB checks stayed healthy. | `docker compose ... start redis`; later health recovered after dependencies stabilized. |
| RabbitMQ down | `docker compose -f infrastructure/local/docker-compose.yml stop rabbitmq` | `/health` returned HTTP 503, `status: Unhealthy`; DB checks stayed healthy. | `docker compose ... start rabbitmq`. |
| SQL Server down | `docker compose -f infrastructure/local/docker-compose.yml stop sqlserver` | `/health` returned HTTP 503; database checks were unhealthy and took about 8.4s. | `docker compose ... start sqlserver`; health recovered to HTTP 200 after about 30s. |

## Not Executed

- Worker failure.
- Network partition beyond container stop.
- Certificate failure.
- DNS failure.
- Disk pressure.
- Memory pressure.

## Verdict

Health checks correctly detect SQL, Redis, and RabbitMQ outages. Recovery is manual and not backed by committed runbook automation.
