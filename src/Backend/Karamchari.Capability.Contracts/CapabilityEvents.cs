using Karamchari.Core.Messaging;

namespace Karamchari.Capability.Contracts;

// The payload for the Skill Achieved event
public sealed record SkillAchievedPayload(
    Guid ProfileId,
    Guid EmployeeId,
    Guid SkillId,
    string Level,
    string VerifiedBy);

// The payload for the Learning Completed event
public sealed record LearningCompletedPayload(
    Guid EnrollmentId,
    Guid EmployeeId,
    Guid ModuleId);

// The payload for the Certification Expired event
public sealed record CertificationExpiredPayload(
    Guid CertificationId,
    Guid EmployeeId,
    string CertificationName);

// The payload for the Growth Plan Activated event
public sealed record GrowthPlanActivatedPayload(
    Guid GrowthPlanId,
    Guid EmployeeId,
    string TargetRole);
