# Phase 1 — Environment Certification

**Date:** 2026-05-29 · **Host:** macOS (arm64) · **Docker:** 29.5.2 · **.NET SDK:** 10.0.300
**Result: ✅ PASS** (infrastructure) — with operational finding on persistence config.

## Method
Started the documented local stack and exercised each dependency with real connections.
```bash
export KARAMCHARI_SQL_PASSWORD='Karamchari@123'
docker compose -f infrastructure/local/docker-compose.yml up -d sqlserver redis rabbitmq seq mailpit
```

## Evidence — Containers
| Service | Image | Status | Port(s) |
|---|---|---|---|
| sqlserver | mcr.microsoft.com/mssql/server:2022-latest | Up | 1433 |
| redis | redis:7-alpine | Up | 6379 |
| rabbitmq | rabbitmq:3-management-alpine | Up | 5672 / 15672 |
| seq | datalust/seq:latest | Up | 8081 |
| mailpit | axllent/mailpit:latest | Up (healthy) | 1025 / 8025 |

## Evidence — Connectivity & Credentials
- **SQL Server**: `SELECT 1` succeeded as `sa`. Version: `Microsoft SQL Server 2022 (RTM-CU25-GDR) 16.0.4260.1`.
  - **Negative credential test**: login with `WrongPass!` → `Login failed for user 'sa'` (auth enforced). ✅
- **Redis**: `PING` → `PONG`. `redis_version:7.4.9`. `CONFIG GET save` → `3600 1 300 100 60 10000` (RDB snapshotting enabled).
- **RabbitMQ**: `rabbitmq-diagnostics ping` → `Ping succeeded`. Version `3.13.7`. vhost `/` present. Management API reachable on 15672 (guest/guest).

## Evidence — Persistence
- SQL Server is backed by named volume `local_sqlserver-data` → `/var/opt/mssql`. Data survives container restart. ✅
- **Finding (LOW):** `redis` and `rabbitmq` services declare **no volumes** in `infrastructure/local/docker-compose.yml`. Redis RDB persistence is configured inside the container but is lost on container removal; RabbitMQ message/definition state is non-durable across `docker compose down`. Acceptable for local dev; must be addressed for any shared/staging use.

## Restart Recovery
- Redis and RabbitMQ were stopped and restarted during Phase 16 (Chaos). The application detected the outage (`/health` → 503) and recovered to `Healthy` (200) after restart without an app restart. See `chaos.md`.

## Verdict
Infrastructure connectivity, credentials, and SQL persistence **PASS**. One LOW finding (no durable volumes for redis/rabbitmq in the compose file).
