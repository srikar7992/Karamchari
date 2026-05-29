# Recruitment Domain

## Extracted Knowledge

| Required Item | Evidence-backed Content |
|---|---|
| Purpose | Manages job requisitions, candidate profiles, candidate applications, interview sessions, and offer letters. Evidence: `src/Backend/Karamchari.Recruitment/Persistence/RecruitmentDbContext.cs:28`. |
| Business Objectives | UNKNOWN beyond hiring pipeline storage implied by entities. |
| Core Concepts | Job requisition, candidate profile, application, interview session, offer letter, workforce demand signal. |
| Aggregates / Entities | DbSets in `RecruitmentDbContext`. Evidence: `src/Backend/Karamchari.Recruitment/Persistence/RecruitmentDbContext.cs:28`. |
| Value Objects | Recruitment primitives. Evidence: `src/Backend/Karamchari.Recruitment/Domain/Primitives/RecruitmentPrimitives.cs`. |
| State Machines | `OfferStatus`, `ApplicationStatus`, `InterviewStatus`, `HiringPriority`, `RequisitionStatus`, `CandidateSource`, workforce demand enums. Evidence: `src/Backend/Karamchari.Recruitment/Domain/**/*.cs`. |
| Events | Recruitment-specific integration events UNKNOWN. |
| Commands | No API endpoints found in this pass. |
| Queries | No API endpoints found in this pass. |
| Business Rules / Invariants / Validation | UNKNOWN from source inventory. |
| Calculation Rules | UNKNOWN. |
| Ownership Rules | Tenant-scoped; recruiter/hiring-manager ownership UNKNOWN. |
| Dependencies | Karamchari Core and persistence. |
| External Integrations | UNKNOWN. |
| Examples | UNKNOWN; no committed recruitment API examples found. |
| Failure Scenarios | UNKNOWN. |
| Known Limitations | No dedicated tests or exposed BFF endpoints found; module is not independently supportable from repository docs. |
