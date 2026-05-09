# Analytical Architecture Report

## 1. Transactional Analytics Coupling
- **Risk:** Current analytical queries (e.g., `ProjectMetrics`, live dashboard counts) scan operational OLTP tables or use in-band EF aggregate tracking. As intelligence queries expand (e.g., historical readiness over 2 years), this will crash the primary database.
- **Mitigation:** Introduce `Analytics Projection` patterns. Use outbox events to asynchronously build denormalized Read Models (`Projections`) stored in dedicated query tables (or a future OLAP store).

## 2. Stale Projection Recovery
- **Risk:** If an outbox consumer generating an analytical projection fails, the read model drifts silently from the operational truth.
- **Mitigation:** Projections must track `LastProcessedOccurredAt` and support deterministic replay logic from the event store to rebuild views from scratch safely.
