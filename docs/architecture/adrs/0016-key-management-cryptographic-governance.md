# ADR-0016 — Key Management & Cryptographic Governance

**Status:** Accepted  
**Date:** 2026-05-30  
**Author:** Gemini CLI

---

## Context

The Execution Context Preservation System (ADR-0015) uses HMAC-SHA256 signatures to ensure the integrity of tenant and audit metadata. This introduces a dependency on a platform-wide secret key. Improper management of this key (e.g., hardcoding, lack of rotation, or loss during failover) would lead to platform-wide availability failures where all integration events are rejected.

---

## Decision

### Key Storage & Lifecycle

1.  **Storage**: Signing secrets must be stored in a secure vault (e.g., Azure Key Vault, HashiCorp Vault). They must never be hardcoded in the codebase or stored in plain-text configuration files.
2.  **Rotation Frequency**: Keys will be rotated every 90 days.
3.  **Rotation Period (The "Dual-Key" Window)**: During rotation, the system will support two valid keys:
    - **Primary Key**: The newest key, used for all new outgoing signatures.
    - **Fallback Key**: The previous key, used for validation only.
4.  **Retention Period**: Fallback keys will be retained for 14 days after a rotation. This provides a buffer for long-lived messages in the Outbox or Dead Letter Queues to be processed or replayed.

### Operational Resilience

1.  **Maximum Replay Age**: The system will include a `MT-Timestamp` in the signed payload. Messages older than 7 days will be rejected regardless of signature validity, unless manually authorized by an operator.
2.  **Blue-Green & Regional Failover**: Both "Blue" and "Green" environments, or both regional replicas, must share access to the same current set of Primary/Fallback keys to ensure messages published in one can be consumed in the other.
3.  **Secret Versioning**: The `MT-Context-Version` header will eventually be used to map signatures to specific key versions if rotation frequency increases.

### Rollback Strategy

In the event of a key compromise or catastrophic failure in the signing pipeline:
1.  Rotate the Primary Key immediately.
2.  Maintain the compromised key as a Fallback only if forensics confirm it is safe to do so for queue draining; otherwise, purge it and accept that in-flight messages will require manual reconciliation.

---

## Consequences

- **Enhanced Security**: Regular rotation limits the impact of a leaked secret.
- **Survivability**: The dual-key window prevents rotation from breaking the platform.
- **Auditability**: Timestamp-based rejection prevents stale message replay attacks.
- **Infrastructure Dependency**: Requires a functional vault integration for automated rotation.
