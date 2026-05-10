# Platform Governance Checklist

Every new platform abstraction or infrastructural change must pass this review before merging. The goal is to prevent Platform Complexity Collapse by enforcing rigorous justification.

## 1. Abstraction Justification
- [ ] **Is this a net-new concept?** If yes, why cannot an existing abstraction (like `TenantExecutionContext`) handle it?
- [ ] **Does it duplicate behavior?** Ensure no overlap with existing scopes or interceptors.
- [ ] **Is it localized?** Does this change leak infrastructure concerns into business logic? (Must be NO).

## 2. Golden Path Compliance
- [ ] **Follows Canonical Strategy?** Does the change adhere to the rules in `golden_paths.md`?
- [ ] **Automated Enforcement?** Has an ArchUnitNET/Roslyn rule been added to `PlatformFitnessTests.cs` to prevent drift?

## 3. Operational Maintainability
- [ ] **Debugging Impact**: Can a Tier 1 support engineer trace the behavior using Seq/Grafana without reading the source code?
- [ ] **Cognitive Load**: Can the concept be explained in a 1-page Mental Model doc? (If not, it is too complex).

## 4. Isolation & Resilience
- [ ] **Chaos Proven?** Does the change survive the `./scripts/chaos/run-chaos.ps1` suite?
- [ ] **Tenant Safe?** Are 100% of the Tenant Isolation Certification tests passing?

## Approval Requirements
Requires PR approval from the **Enterprise Systems Simplification Lead** or a **Principal Platform Architect**.
