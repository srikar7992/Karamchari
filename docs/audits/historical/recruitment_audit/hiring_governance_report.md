# Hiring Governance Report

## 1. Workflow Escalation & Compensation
- **Compensation Loopholes:** Offers extending beyond standard bands without explicit executive approval represent a major financial compliance risk.
- **Mitigation:** Implement `CompensationProposal` as part of an explicit `HiringApprovalWorkflow`. Offers cannot be extended until the workflow reaches an `Approved` state.

## 2. Unstructured Interview Bias
- **Feedback Governance:** Open-text interview feedback with no lock-in allows for retroactive justification of biased hiring decisions.
- **Mitigation:** `InterviewFeedback` must be locked upon submission. Scorecards must follow standardized templates tied to the `InterviewPlan`.

## 3. Cross-Module Coupling
- **Onboarding Coupling:** Transitioning a candidate to an employee must not involve deep integration.
- **Mitigation:** The Recruitment bounded context will only emit a `HiringDecisionCompleted` event. The HR context will consume this to initiate onboarding asynchronously.
