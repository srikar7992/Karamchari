# WS13 — Durable Infrastructure

**Status: ✅ CLOSED (was LOW).**

## Changes implemented (`infrastructure/local/docker-compose.yml`)
Named volumes added for every stateful service:
| Service | Volume | Mount |
|---|---|---|
| sqlserver | `sqlserver-data` | `/var/opt/mssql` (pre-existing) |
| redis | `redis-data` | `/data` (+ `--appendonly yes`) |
| rabbitmq | `rabbitmq-data` | `/var/lib/rabbitmq` |
| seq | `seq-data` | `/data` (+ `SEQ_FIRSTRUN_NOAUTHENTICATION`) |
| prometheus | `prometheus-data` | `/prometheus` |
| grafana | `grafana-data` | `/var/lib/grafana` |

## Verification — data survives container restart
```
docker volume ls -> local_{sqlserver,redis,rabbitmq,seq,prometheus,grafana}-data  (all present)

Redis:   SET cert:durability "survives-restart"; SAVE; docker restart redis
         GET cert:durability  -> "survives-restart"        ✅
SQL:     docker restart sqlserver
         SELECT COUNT(*) FROM [identity].AspNetUsers -> 4   ✅ (users persisted)
```

## Verdict
Durable Infrastructure = **PASS** — Redis (AOF) and SQL Server data survive restarts; all stateful services have named volumes.
