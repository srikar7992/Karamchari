# Candidate Privacy Risk Report

## 1. PII Protection
- **Candidate Resumes & Data:** ATS contains highly sensitive personal, contact, and compensation history data.
- **Mitigation:** Store attachments safely. Explicitly tag `CandidateProfile` PII. Adhere to "Right to be Forgotten" (GDPR/CCPA) via structured data retention policies.

## 2. Confidential Requisitions
- **Executive Hiring Leaks:** Executive or replacement requisitions visible to standard recruiters or internal employees violate operational security.
- **Mitigation:** Introduce `HiringPriority` and confidential flags. Enforce strict role-based access to `JobRequisition` aggregates.
