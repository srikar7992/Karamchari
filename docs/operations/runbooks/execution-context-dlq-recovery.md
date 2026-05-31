# Runbook: Execution Context DLQ Recovery

## Overview
This runbook defines the process for recovering messages from the Dead Letter Queue (DLQ) that failed due to signature validation or missing tenant context.

## RTO / SLA
- **Target Time**: < 30 minutes.

## Prerequisites
- Access to the RabbitMQ Management UI or CLI.
- Access to the `HRDbContext` / `PayrollDbContext` for event reconciliation.

## Procedure

### 1. Identify Failure Reason
1. Inspect the message headers in the DLQ.
2. Look for the `MT-Context-Signature` and `MT-Tenant-Id`.
3. Check the `MT-Fault-Reason` (if present) or cross-reference with Worker logs using the `CorrelationId`.
4. Common Reasons:
   - **InvalidSignature**: Key rotation window passed, or message was tampered.
   - **MissingTenantId**: Event was published by an unpatched producer (v1).
   - **StaleMessage**: Message age exceeded TTL.

### 2. Reconciliation Strategy
Depending on the reason, choose one:

#### Strategy A: Re-sign and Re-publish
1. If the message is legitimate but the signature is invalid (e.g., extremely old message replayed from backup), use the `PreservationRecoveryTool` (if implemented) to generate a new valid signature using the **Current Primary Key**.
2. Re-publish the corrected message to the primary exchange.

#### Strategy B: Move to Primary Exchange (Key Rotation Only)
1. If the message failed during a key rotation because the Fallback Key was removed too early, temporarily restore the old key as a fallback.
2. Move all messages from DLQ back to the primary queue.
3. Once drained, remove the fallback key again.

### 3. Verify Recovery
1. Monitor the Worker logs for successful processing.
2. Confirm the data landed in the correct tenant schema.

## Rollback Plan
If re-published messages continue to fail and flood the DLQ, stop the recovery process immediately and investigate if the `PreservationRecoveryTool` is using the correct signing canonicalization.
