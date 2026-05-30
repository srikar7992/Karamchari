# Messaging Metadata Governance Checklist

## Integration Event Definition
- [ ] Event record is defined in `Karamchari.Core.Contracts`.
- [ ] Event includes a `TenantId` property in its payload for business-level validation.

## Infrastructure Certification
- [ ] Producer correctly stamps `MT-Tenant-Id` (verified via `OutboxMessage` inspection).
- [ ] Producer correctly signs context (verified via `MT-Context-Signature` presence).
- [ ] Metadata survives transport (verified via `ConsumeContext.Headers`).
- [ ] Consumer rehydrates `TenantExecutionContext` (verified via `TenantConsumeFilter` logs).

## Security & Resilience
- [ ] Signature validation is enforced (tampering rejected).
- [ ] Replay protection is functional (duplicate `MessageId` rejected).
- [ ] DLQ preservation verified (failed messages keep original headers).

## CI Enforcement
- [ ] A regression test exists in the bounded context or platform suite proving propagation for this specific event type.
- [ ] Architecture tests pass (no domain bypass).
