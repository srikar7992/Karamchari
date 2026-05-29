# Health Checks & Probes Specification

This document defines the liveness, readiness, and startup health probe configurations used by container orchestrators (e.g., Kubernetes) and load balancers to monitor the Karamchari platform.

---

## 1. Health Probe Endpoints

All probes are registered in `HealthCheckExtensions.cs` and are exposed on the API gateway host:

| Endpoint Path | Probe Type | Checked Dependencies | Failure Behavior |
| :--- | :--- | :--- | :--- |
| `/health/live` | **Liveness Probe** | None. Confirms that the host process is running. | Processes stay alive; always returns HTTP 200. |
| `/health/ready` | **Readiness Probe** | 14 main domain DbContexts, Redis connection, and RabbitMQ connection. | If any check fails, returns HTTP 503 (Unhealthy). The pod is taken out of service. |
| `/health/startup` | **Startup Probe** | DB Migrations status, all 14 DbContexts, Redis, and RabbitMQ. | Blocks traffic entry during boot. Returns HTTP 503 if dependencies are offline during warmup. |
| `/health` | **Aggregate Check** | Detailed status JSON containing result objects for all checks. | Primarily used by operational CLI verification scripts and smoke tests. |

---

## 2. Validation & Recovery Behavior

In containerized deployments:
1.  **Liveness Isolation**: If a transient database outage occurs, `/health/ready` and `/health/startup` will transition to HTTP 503 (Degraded/Unhealthy), but `/health/live` remains HTTP 200. This prevents the orchestrator from killing and restarting the API gateway process during a brief database failover.
2.  **Startup Probe Gate**: Prevents HTTP traffic routing until all multi-tenant SQL migrations, schemas, RLS policies, and signing key provisions are ready.
3.  **Local Smoke Testing**: Run the following from the host to verify all checks are green:
    ```bash
    ./verify-local.sh
    ```
