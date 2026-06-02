# Analytics Certification

Date: 2026-06-02
Status: CERTIFIED

## Consumers Implemented
- RecruitmentVelocityConsumer: consumes RequisitionCreated
- TimeToHireConsumer: consumes CandidateHired, OfferAccepted
- HiringFunnelConsumer: consumes ApplicationSubmitted, InterviewCompleted, OfferAccepted

## AnalyticsReadModel
- Entity created with migration
- Persisted to Recruitment database

## Idempotency
- Duplicate event check before insert (EventType + entity IDs)

## Verification Method
Execute full recruitment journey -> query AnalyticsReadModel -> verify rows materialized per event

## Result
CERTIFIED -- consumers implemented, entity persisted, idempotency enforced
