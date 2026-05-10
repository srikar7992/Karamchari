# Operating Mode: Lightweight

**Purpose**: High-velocity feature development.

## Infrastructure Stack
- SQL Server (Core + Multi-schema)
- Redis (Caching only)
- RabbitMQ (InMemory mode)
- Seq (Minimal logging)

## Features Disabled
- Full distributed tracing (OTEL Collector)
- Prometheus & Grafana
- Chaos Engineering Suite
- Certification soak tests

## Commands
```powershell
# Start minimal infra
docker compose -f infrastructure/local/docker-compose.yml up -d sqlserver redis seq
# Start API with InMemory messaging
$env:ConnectionStrings__RabbitMQ=""
dotnet watch run --project src/Backend/Karamchari.Api
```
