# Operating Mode: Certification

**Purpose**: Formal validation of tenant isolation and platform contracts.

## Infrastructure Stack
- Full Docker Stack (SQL, Redis, RabbitMQ, Azurite)
- OTEL Collector enabled
- Seq enabled

## Features Enabled
- RLS Verification
- Replay Storms
- Retry Storms
- Integration Tests

## Commands
```powershell
./scripts/validation/validate-runtime.ps1
```
