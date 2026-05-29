# Runbook: Replay & Retry Recovery

**Status**: EXPLAINABLE

## Scenario 1: Message in Dead-Letter Queue (DLQ)
**Detection**: Alert in Seq for `MassTransit.PoisonMessage`.
**Procedure**:
1.  Inspect the message headers for `X-Tenant-Id`.
2.  Search Seq for the `CorrelationId` to see why it failed.
3.  Fix the underlying data/logic issue.
4.  Use RabbitMQ Management UI to move the message back to the primary queue.
5.  Observe Seq: the `ReplayProtectionService` will allow the message *if* it was never successfully recorded.

## Scenario 2: Persistent Retry Storm
**Detection**: Database CPU spikes; Seq logs show hundreds of `TenantSessionContextMismatchException`.
**Procedure**:
1.  Identify the "Storming Tenant" via Grafana.
2.  Pause the specific consumer queue for that tenant.
3.  Wipe the Redis cache for that tenant if data corruption is suspected.
4.  Resume execution with increased backoff intervals.

## Scenario 3: Connection Contamination Alert
**Detection**: `PooledConnectionContaminationException` in Seq.
**Procedure**:
1.  Search Seq for `karamchari.correlation.id` of the failure.
2.  Identify the exact query that failed.
3.  Check the stack trace for `async Task` without `await` or manual thread spawning.
4.  Fix the leak and restart the service node to flush the connection pool.
