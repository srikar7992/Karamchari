namespace Karamchari.Approvals.Domain;

public enum ApprovalStepResolverType
{
    DirectManager = 1,
    SpecificUser = 2,
    Role = 3,
    HRBPOfEmployee = 4
}

public enum ApprovalPolicyMode
{
    Sequential = 1,
    Parallel = 2,
    Quorum = 3
}

public sealed record ApprovalPolicyStep(
    int StepOrder,
    ApprovalStepResolverType ResolverType,
    Guid? SpecificUserId,
    string? RoleName,
    int SlaHours);
