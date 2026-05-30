# Runbook: Execution Context Staging Certification

## Overview
This runbook defines the mandatory certification steps to be performed in the Staging environment before the Execution Context Preservation System can be promoted to Production.

## Prerequisites
- [ ] MassTransit generic filters deployed to all Staging replicas (API and Workers).
- [ ] `Messaging:SigningSecret` provisioned in Staging Key Vault.
- [ ] Prometheus/Grafana configured to scrape `Karamchari.TenantIsolation` metrics.

## Certification Steps

### 1. Chain of Custody Verification
1.  Trigger a `SyntheticPingIntegrationEvent` via the internal Staging CLI or API.
2.  **Verify Outbox**: Query the SQL Staging instance:
    ```sql
    SELECT TOP 1 [Headers] FROM [HR].[OutboxMessages] ORDER BY [EnqueuedTime] DESC
    ```
    Confirm JSON contains `MT-Tenant-Id` and `MT-Context-Signature`.
3.  **Verify Consumer**: Check Worker logs for:
    `Tenant context established and signature validated for Tenant staging-test`

### 2. Failure Visibility (Alerting)
1.  Manually publish a message to RabbitMQ with a modified `MT-Tenant-Id` but keeping the same signature (Tamper simulation).
2.  **Verify Rejection**: Confirm worker logs `Message cryptographic signature is invalid`.
3.  **Verify Alert**: Ensure the Prometheus alert `TenantSignatureValidationFailed` fires and reaches the PagerDuty/Slack channel configured for Staging.

### 3. Replay Protection
1.  Locate a successfully processed message ID in the `InboxState` table.
2.  Manually re-publish the same message to the exchange.
3.  **Verify Rejection**: Confirm worker logs `Message appears to be a replay and will be rejected`.

### 4. Dashboards Sign-off
- [ ] **Tenant Propagation Dashboard**: Confirm all tenants show successful context restoration.
- [ ] **SLO Dashboard**: Confirm `tenant_event_rejection_rate` is 0% for legitimate traffic.
- [ ] **Latency Dashboard**: Confirm `tenant_event_processing_latency` overhead matches baseline performance tests (< 0.1ms delta).

## Sign-off
Certification of the above steps is required for Production rollout.
