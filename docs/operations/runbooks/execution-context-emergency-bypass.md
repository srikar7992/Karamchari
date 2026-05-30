# Runbook: Execution Context Emergency Bypass

## Overview
This runbook defines the process for disabling execution context signature validation in the event of a catastrophic system failure (e.g., global key desync, cryptographic algorithm bug) that blocks all platform messaging.

## RTO / SLA
- **Target Time**: < 10 minutes.
- **Security Impact**: HIGH (Tenant isolation is weakened during bypass). Requires CISO authorization.

## Prerequisites
- Permission to update Application Configuration.
- Approval from CISO or Head of Engineering.

## Procedure

### 1. Enable Audit Mode
1. Change the configuration setting `Messaging:ExecutionContextValidationMode` from `Enforce` to `AuditOnly`.
2. This change disables the **rejection** of invalid signatures.
3. The `TenantConsumeFilter` will continue to log `Signature validation failed` warnings and increment metrics, but will allow message processing to continue with the provided `TenantId`.

### 2. Verify Processing
1. Verify that the Dead Letter Queue (DLQ) growth stops.
2. Confirm that business-critical events (Payroll, Billing) are being processed again.
3. **Warning**: Monitor for any potential cross-tenant leakage manually during this window.

### 3. Resolve Root Cause
1. While in `AuditOnly` mode, identify the root cause (e.g., fix the Key Vault rotation script, revert a buggy filter deployment).
2. Restore the correct signing/validation state.

### 4. Restore Enforce Mode
1. Once the system is confirmed healthy, change `Messaging:ExecutionContextValidationMode` back to `Enforce`.
2. Verify that `tenant_event_rejection_rate` remains 0% for legitimate traffic.

## Rollback Plan
If `AuditOnly` mode creates unacceptable data integrity risks, revert to `Enforce` and accept the availability loss while a deeper fix is implemented.
