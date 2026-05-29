# Explainability Readiness Report

## 1. Black-box Scoring Risks
- **Risk:** As workforce intelligence advances toward predictive models (Succession Readiness, Flight Risk), providing a raw `Score` without context will lead to rejection by HR leaders or legal challenges regarding bias.
- **Mitigation:** Implement `ScoreExplanation` structures. Every strategic score must include:
  - Base signals used
  - Weighting applied
  - Confidence degradation factors
  - Explicit list of missing/null inputs that dragged down the score.
  
## 2. Human Oversight Boundaries
- **Risk:** Automated career penalties based on derived scores.
- **Mitigation:** Intelligence aggregates are strictly marked as `Advisory`. The architecture must mandate a `HumanReview` threshold before an intelligence signal can trigger an operational action (e.g., demotion or block from promotion).
