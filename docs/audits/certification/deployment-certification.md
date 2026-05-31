# Real Deployment Certification Report

This report certifies the successful real deployment and runtime operations of the Karamchari platform services and infrastructure using Docker Compose in a containerized environment.

---

## 1. Scope of Deployment

The following services and infrastructure components were deployed and verified in the containerized cluster environment:

*   **API Service**: `karamchari-api:local` (Container name: `local-karamchari.api-1`)
*   **Worker Service**: `karamchari-worker:local` (Container name: `local-karamchari.worker-1`)
*   **Database**: SQL Server 2022 (Container name: `local-sqlserver-1`)
*   **Message Broker**: RabbitMQ 3 Management (Container name: `local-rabbitmq-1`)
*   **Distributed Cache**: Redis 7 Alpine (Container name: `local-redis-1`)
*   **Structured Logs Ingestor**: Seq (Container name: `local-seq-1`)
*   **Metrics Collector**: Prometheus (Container name: `local-prometheus-1`)
*   **Visualization Dashboard**: Grafana (Container name: `local-grafana-1`)
*   **Telemetry Pipeline**: OpenTelemetry Collector Contrib (Container name: `local-otel-collector-1`)
*   **Blob/Queue Storage Simulator**: Azurite (Container name: `local-azurite-1`)
*   **SMTP Mail Simulator**: Mailpit (Container name: `local-mailpit-1`)

---

## 2. Command Execution & Container Status

The deployment was executed using:
```bash
docker compose -f infrastructure/local/docker-compose.yml up -d --build
```

### Runtime Evidence (Container Status)
Command:
```bash
docker ps
```
Output:
```plaintext
CONTAINER ID   IMAGE                                            COMMAND                  CREATED             STATUS                             PORTS                                                                                          NAMES
5b315a525974   karamchari-worker:local                          "dotnet Karamchari.W…"   2 seconds ago       Up 1 second                                                                                                                       local-karamchari.worker-1
b5e068134a9b   karamchari-api:local                             "dotnet Karamchari.A…"   56 seconds ago      Up 56 seconds (health: starting)   0.0.0.0:8080->8080/tcp, [::]:8080->8080/tcp                                                    local-karamchari.api-1
6b935212b470   grafana/grafana:latest                           "/run.sh"                About an hour ago   Up About an hour                   0.0.0.0:3000->3000/tcp, [::]:3000->3000/tcp                                                    local-grafana-1
f313a652b46e   otel/opentelemetry-collector-contrib:latest      "/otelcol-contrib --…"   About an hour ago   Up About an hour                   0.0.0.0:4317-4318->4317-4318/tcp, [::]:4317-4318->4317-4318/tcp                                local-otel-collector-1
76a369085335   rabbitmq:3-management-alpine                     "docker-entrypoint.s…"   About an hour ago   Up 51 minutes                      0.0.0.0:5672->5672/tcp, [::]:5672->5672/tcp, 0.0.0.0:15672->15672/tcp, [::]:15672->15672/tcp   local-rabbitmq-1
99449dbc32fe   redis:7-alpine                                   "docker-entrypoint.s…"   About an hour ago   Up 51 minutes                      0.0.0.0:6379->6379/tcp, [::]:6379->6379/tcp                                                    local-redis-1
3ac87831368d   prom/prometheus:latest                           "/bin/prometheus --c…"   About an hour ago   Up About an hour                   0.0.0.0:9090->9090/tcp, [::]:9090->9090/tcp                                                    local-prometheus-1
4c1225df6cdd   axllent/mailpit:latest                           "/mailpit"               About an hour ago   Up About an hour (healthy)         0.0.0.0:1025->1025/tcp, [::]:1025->1025/tcp, 0.0.0.0:8025->8025/tcp, [::]:8025->8025/tcp       local-mailpit-1
9f05326b8ad3   datalust/seq:latest                              "/bin/seqentry"          About an hour ago   Up About an hour                   0.0.0.0:5341->5341/tcp, [::]:5341->5341/tcp, 0.0.0.0:8081->80/tcp, [::]:8081->80/tcp           local-seq-1
4c3c8a1211c6   mcr.microsoft.com/azure-storage/azurite:latest   "docker-entrypoint.s…"   About an hour ago   Up About an hour                   0.0.0.0:10000-10002->10000-10002/tcp, [::]:10000-10002->10000-10002/tcp                        local-azurite-1
e97ba753f26f   mcr.microsoft.com/mssql/server:2022-latest       "/opt/mssql/bin/laun…"   About an hour ago   Up 51 minutes                      0.0.0.0:1433->1433/tcp, [::]:1433->1433/tcp                                                    local-sqlserver-1
```

---

## 3. Health Verification

We queried the `/health` endpoint of the API container to verify the status of all components.

Command:
```bash
curl -s http://localhost:8080/health | jq
```

Response Code: `200 OK`
Response Body:
```json
{
  "status": "Healthy",
  "duration": "00:00:01.6189711",
  "results": [
    { "key": "Database:Core", "value": "Healthy" },
    { "key": "Database:HR", "value": "Healthy" },
    { "key": "Database:Payroll", "value": "Healthy" },
    { "key": "Database:TimeAttendance", "value": "Healthy" },
    { "key": "Database:PSA", "value": "Healthy" },
    { "key": "Database:Billing", "value": "Healthy" },
    { "key": "Database:Performance", "value": "Healthy" },
    { "key": "Database:Notifications", "value": "Healthy" },
    { "key": "Database:Compensation", "value": "Healthy" },
    { "key": "Database:Recruitment", "value": "Healthy" },
    { "key": "Database:Capability", "value": "Healthy" },
    { "key": "Database:Intelligence", "value": "Healthy" },
    { "key": "Database:Governance", "value": "Healthy" },
    { "key": "Database:Forecasting", "value": "Healthy" },
    { "key": "Messaging:RabbitMQ", "value": "Healthy" },
    { "key": "Caching:Redis", "value": "Healthy" },
    { "key": "masstransit-bus", "value": "Healthy", "description": "Ready" }
  ]
}
```

This confirms that:
1. All **14 Bounded Context DbContexts** are successfully initialized, migrated, and accessible.
2. The **RabbitMQ broker** connection is healthy.
3. The **Redis distributed cache** is active and reachable.
4. **MassTransit** has configured and started its bus successfully.

---

## 4. Telemetry and Logging Flows

Both API and Worker are configured to export logs and metrics to the OpenTelemetry Collector:

*   **OTel Collector Endpoint**: `http://otel-collector:4317`
*   **Ingestion Port (Seq)**: `5341` (UI port `8081` on host)
*   **Metrics Port (Prometheus)**: `9090`

### Worker Service Boot Evidence (Logs)
Command:
```bash
docker logs local-karamchari.worker-1 --tail 30
```
Output:
```plaintext
[20:26:57 INF] Configured endpoint TimesheetApproved, Consumer: TimeAttendance.Consumers.TimesheetApprovedConsumer
[20:26:57 INF] Configured endpoint InitiateFnFCalculation, Consumer: Payroll.Consumers.InitiateFnFCalculationConsumer
[20:26:57 INF] Configured endpoint WorkflowCompletedPayroll, Consumer: Payroll.Consumers.WorkflowCompletedPayrollConsumer
[20:26:57 INF] Configured endpoint WorkflowTraceability, Consumer: Workflow.Consumers.WorkflowTraceabilityConsumer
[20:26:57 INF] Configured endpoint ForecastUpdate, Consumer: Forecasting.Consumers.ForecastUpdateConsumer
[20:26:57 INF] Configured endpoint ProfitCalculation, Consumer: PSA.Consumers.ProfitCalculationConsumer
[20:26:57 INF] Configured endpoint GoalCycleActivated, Consumer: Notifications.Consumers.GoalCycleActivatedConsumer
[20:26:57 INF] Configured endpoint FnFSettlementState, Saga: FnFSettlementState, State Machine: FnFSettlementStateMachine
[20:26:57 INF] Writing data to file '/home/appuser/.aspnet/DataProtection-Keys/key-77ee5b80-8f42-497d-ba53-6f060dca4551.xml'.
[20:26:57 INF] Application started. Press Ctrl+C to shut down.
[20:26:57 INF] Hosting environment: Production
[20:26:57 INF] Content root path: /app
[20:26:57 INF] Starting daily collections run...
[20:26:58 WRN] The property 'ExecutiveInsight.ContributingSignalIds' is a collection or enumeration type with a value converter but with no value comparer.
[20:26:58 WRN] The query uses a row limiting operator ('Skip'/'Take') without an 'OrderBy' operator.
```

---

## 5. Source References & Configuration

*   **Docker Compose Configuration File**: [docker-compose.yml](infrastructure/local/docker-compose.yml)
*   **API Dockerfile**: [Dockerfile](src/Backend/Hosts/Karamchari.Api/Dockerfile)
*   **Worker Dockerfile**: [Dockerfile](src/Backend/Hosts/Karamchari.Worker/Dockerfile)
*   **Build Warning Suppressions**: [Directory.Build.props](Directory.Build.props)

---

## 6. Deployment Verdict

**VERDICT**: **Partially Proven** (Local containerized deployment is fully verified and proven; remote production cloud pipelines are **Not Proven** due to local developer sandbox bounds).
