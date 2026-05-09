using Karamchari.Core.Messaging;

namespace Karamchari.Capability.Contracts;

// The payload for the Skill Achieved event
/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed record SkillAchievedPayload(
    Guid ProfileId,
    Guid EmployeeId,
    Guid SkillId,
    string Level,
    string VerifiedBy);

// The payload for the Learning Completed event
/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed record LearningCompletedPayload(
    Guid EnrollmentId,
    Guid EmployeeId,
    Guid ModuleId);

// The payload for the Certification Expired event
/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed record CertificationExpiredPayload(
    Guid CertificationId,
    Guid EmployeeId,
    string CertificationName);

// The payload for the Growth Plan Activated event
/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed record GrowthPlanActivatedPayload(
    Guid GrowthPlanId,
    Guid EmployeeId,
    string TargetRole);
