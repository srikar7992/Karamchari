# Capability Domain

## Extracted Knowledge

| Required Item | Evidence-backed Content |
|---|---|
| Purpose | Manages skill definitions, capability profiles, learning modules/enrollments, certifications, growth plans, capability definitions, and tenant capabilities. Evidence: `src/Backend/Karamchari.Capability/Persistence/CapabilityDbContext.cs:27`. |
| Business Objectives | UNKNOWN beyond skills, learning, certifications, growth plans, and tenant entitlements. |
| Core Concepts | Skill, capability profile, learning module, enrollment, certification achievement, growth plan, capability entitlement. |
| Aggregates / Entities | DbSets in `CapabilityDbContext`. |
| Value Objects | UNKNOWN. |
| State Machines | `SkillLevel`, `CompetencyRating`, `WorkforceReadinessLevel`, `EnrollmentStatus`, `CertificationStatus`, `GrowthPlanStatus`, `CapabilityStatus`, `CapabilityTier`. Evidence: `src/Backend/Karamchari.Capability/Domain/Primitives/CapabilityEnums.cs`, `src/Backend/Karamchari.Capability/Domain/Entitlements/*.cs`. |
| Events | Capability events file exists. Evidence: `src/Backend/Karamchari.Capability.Contracts/CapabilityEvents.cs`. |
| Commands | Define skill, add verified skill, complete enrollment endpoints. Evidence: `src/Backend/Karamchari.Api/BFF/Capability/CapabilityEndpoints.cs:32`, `src/Backend/Karamchari.Api/BFF/Capability/CapabilityEndpoints.cs:41`, `src/Backend/Karamchari.Api/BFF/Capability/CapabilityEndpoints.cs:50`. |
| Queries | List skills, get my profile, list learning modules. Evidence: `src/Backend/Karamchari.Api/BFF/Capability/CapabilityEndpoints.cs:28`. |
| Business Rules / Invariants / Validation | UNKNOWN. |
| Calculation Rules | Readiness levels exist; formula UNKNOWN. |
| Ownership Rules | Tenant capabilities imply tenant-level entitlements; authority UNKNOWN. |
| Dependencies | Core, API BFF. |
| External Integrations | UNKNOWN. |
| Examples | `GET /api/v1/capability/skills`, `POST /api/v1/capability/skills`, `GET /api/v1/capability/profiles/me`. |
| Failure Scenarios | UNKNOWN. |
| Known Limitations | No dedicated tests found. |
