# Runbook: Execution Context Key Rotation

## Overview
This runbook defines the process for rotating the HMAC-SHA256 signing keys used by the Execution Context Preservation System. Regular rotation (every 90 days) minimizes the impact of potential secret leakage.

## RTO / SLA
- **Target Time**: < 15 minutes.
- **Dual-Key window**: Previous key must be retained as fallback for 14 days.

## Prerequisites
- Access to the Production Key Vault (e.g., Azure Key Vault).
- Permission to update Application Configuration (e.g., App Configuration, Environment Variables).

## Procedure

### 1. Generate New Secret
1. Generate a cryptographically secure 64-character string.
   ```bash
   openssl rand -base64 48
   ```

### 2. Update Key Vault (Phase 1: Validation Add)
1. Add the new secret to the Key Vault as the **newest** entry in the `Messaging:SigningSecrets` list.
2. Ensure the **previous** secret is still present in the list.
3. The `ExecutionContextSigner` will automatically pick up both keys:
   - The first key (new) will be used for **SIGNING** and **VALIDATION**.
   - The second key (old) will be used for **VALIDATION ONLY**.

### 3. Verify Staging/Canary
1. Before applying to 100% production, rotate in Staging.
2. Monitor `tenant_event_rejection_rate` in Grafana. It should remain 0%.
3. Verify that messages published *just before* the rotation are still successfully consumed.

### 4. Phase 2: Cleanup (14 days later)
1. After the 14-day retention period, remove the **oldest** secret from the Key Vault.
2. The system now only validates against the new primary key.

## Rollback Plan
If rejections spike after adding the new key:
1. Revert the Key Vault entry to the previous single-key state.
2. Restart the Workers to flush the configuration cache.
3. Investigate if the new key was correctly formatted or if there was a desync in deployment.
