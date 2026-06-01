# Runtime Evidence Matrix
**Date:** 2026-06-01  
**Status:** NOT CERTIFIED (Infrastructure Required)

---

## Required Journey

The full runtime journey requires: SQL Server + RabbitMQ + running API.

| Step | Endpoint | Expected Response | Evidence Status |
|---|---|---|---|
| 1. Login as Recruiter | POST /api/identity/login | JWT + refresh token | REQUIRES INFRA |
| 2. Create Requisition | POST /api/recruitment/requisitions | 201 Created, requisitionId | REQUIRES INFRA |
| 3. Approve Requisition | POST /api/recruitment/requisitions/{id}/approve | 200 OK | REQUIRES INFRA |
| 4. Publish Requisition | POST /api/recruitment/requisitions/{id}/publish | 200 OK | REQUIRES INFRA |
| 5. Create Candidate | POST /api/recruitment/candidates | 201 Created, candidateId | REQUIRES INFRA |
| 6. Apply | POST /api/recruitment/applications | 201 Created, applicationId | REQUIRES INFRA |
| 7. Advance to Interviewing | POST /api/recruitment/applications/{id}/advance | 200 OK | REQUIRES INFRA |
| 8. Schedule Interview | POST /api/recruitment/interviews | 201 Created | REQUIRES INFRA |
| 9. Submit Feedback | POST /api/recruitment/interviews/{id}/feedback | 200 OK | REQUIRES INFRA |
| 10. Create Offer | POST /api/recruitment/offers | 201 Created | REQUIRES INFRA |
| 11. Approve Offer | POST /api/recruitment/offers/{id}/approve | 200 OK | REQUIRES INFRA |
| 12. Issue Offer | POST /api/recruitment/offers/{id}/issue | 200 OK | REQUIRES INFRA |
| 13. Accept Offer | POST /api/recruitment/offers/{id}/accept | 200 OK | REQUIRES INFRA |
| 14. Hire | POST /api/recruitment/hire | 200 OK | REQUIRES INFRA |
| 15. Verify CandidateHiredIntegrationEvent in outbox | SELECT * FROM dbo.OutboxMessage | Row exists | REQUIRES INFRA |
| 16. Verify Employee created in HR | SELECT * FROM [tenant].Employees | Employee row | REQUIRES INFRA |

## Static Evidence (Available Now)

The full journey is verified statically by:
- `RecruitmentApiCertificationTests.VerticalSlice_EndToEnd_ShouldSucceed` — in-memory DB, end-to-end
- Domain tests for each aggregate state transition
- Consumer tests for CandidateHired → Employee creation

## Certification Decision

**NOT CERTIFIED** — requires live infrastructure. Static evidence via integration tests is comprehensive.
