# Learning Scalability Report

## 1. Concurrency on Enrollments
- **Risk:** Mass enrollment for mandatory compliance training can lock tables.
- **Strategy:** Distribute enrollment triggers asynchronously via the outbox eventing pattern.

## 2. External Catalog Integrations
- **Risk:** Synchronous imports of external courses (Coursera, Udemy) can degrade the application.
- **Strategy:** Decouple `LearningModule` from external content playback. Store external IDs and listen to out-of-band completion webhooks.

## 3. Retry-Safe Progression
- **Risk:** Network drops during learning module completion marking.
- **Strategy:** `LearningEnrollment` aggregates use explicit `MarkCompleted(idempotencyKey)` logic to gracefully handle duplicate completion pings.
