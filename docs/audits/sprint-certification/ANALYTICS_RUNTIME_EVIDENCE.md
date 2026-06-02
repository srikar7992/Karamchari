# Analytics Runtime Evidence

Date: 2026-06-02
Test: tests/Backend/Karamchari.Recruitment.Tests/Analytics/AnalyticsRuntimeTests.cs

## Journey Executed
RequisitionCreated -> ApplicationSubmitted -> InterviewCompleted -> OfferAccepted -> CandidateHired

## AnalyticsReadModel Rows Materialized
| EventType | Count | Verified |
|-----------|-------|---------|
| RequisitionCreated | 1 | yes |
| ApplicationSubmitted | 1 | yes |
| InterviewCompleted | 1 | yes |
| OfferAccepted | 1 | yes |
| CandidateHired | 1 | yes |
Total: 5

## Duplicate Suppression
Same CandidateHired event delivered twice -> 1 row (not 2): PASS

## Replay Behavior
3 events dispatched x 2 replays -> 3 rows (not 6): PASS

## Status: CERTIFIED
