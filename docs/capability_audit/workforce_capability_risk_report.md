# Workforce Capability Risk Report

## 1. Skill Fragmentation & Ownership
- **Risk:** Uncontrolled skill tags (e.g. "C#", "C-Sharp", ".NET") dilute capability mapping.
- **Mitigation:** Implement a strict, tenant-scoped `SkillDefinition` aggregate. Skills cannot be arbitrarily assigned without existing in the taxonomy.

## 2. Subjective Assessment Bias
- **Risk:** Manager evaluations of workforce readiness are often highly subjective and lack evidence.
- **Mitigation:** Assessments must require `SkillEvidence` linked to `CompetencyFramework` scoring grids.

## 3. Career Stagnation Tracking
- **Risk:** Mentorship or mobility programs that are not auditable allow invisible talent stagnation.
- **Mitigation:** Establish `GrowthPlan` as a state-machine aggregate tracking active milestones, mentor capacity limits, and expected advancement timelines.
