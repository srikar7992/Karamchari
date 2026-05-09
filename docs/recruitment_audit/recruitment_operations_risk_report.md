# Recruitment Operations Risk Report

## 1. Workforce Demand Integration Risks
- **Mismatch Risk:** Requisitions created without checking active workforce capacity, attrition signals, or budget utilization could lead to overhiring or unauthorized spend.
- **Mitigation:** Requisitions must require explicit `WorkforceDemandSignal` linkage or formal finance/budget approval blocks before publishing.

## 2. Interview Orchestration Complexity
- **Timezone Fragmentation:** Candidates and interviewers in different time zones lead to severe missed-interview risks if local boundaries aren't strictly managed in UTC.
- **Mitigation:** All `InterviewSession` aggregates must enforce UTC storage and calculate overlaps using interviewer-local timezone rules.

## 3. Recruiter Concurrency Risks
- **Duplicate Candidates:** High-volume hiring often results in multiple recruiters interacting with the same candidate profile simultaneously.
- **Mitigation:** Strong aggregate boundaries and `CandidateIdentity` uniqueness checks. Optimistic concurrency (`RowVersion`) required for pipeline transitions to prevent state corruption.
