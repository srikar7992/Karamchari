# Skill Governance Report

## 1. Taxonomy Control
- Skills must be curated. A global `SkillTaxonomy` per tenant will serve as the source of truth.
- `SkillLevel` will follow a standardized scale (e.g., Novice, Intermediate, Advanced, Expert) enforced system-wide to ensure `ReadinessScore` calculations are deterministic.

## 2. Evidence-Based Validation
- Endorsements without backing context are prohibited. `CapabilityProfile` additions require validation workflows where `SkillEvidence` (e.g., certifications, project links, formal assessments) is approved by authorized roles.

## 3. Expiration & Decay
- Certifications expire. `CertificationAchievement` must track an `ExpiresAtUtc` date, publishing events before expiration to trigger compliance warnings.
