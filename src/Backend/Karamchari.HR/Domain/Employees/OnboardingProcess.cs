using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.HR.Domain.Employees;

public enum OnboardingStatus
{
    NotStarted = 1,
    InProcess = 2,
    Completed = 3,
    Abandoned = 4
}

/// <summary>
/// Domain aggregate tracking the business truth of an employee's onboarding journey.
/// Coordination (tasks, approvals) is managed by Workflow.
/// </summary>
public sealed class OnboardingProcess : AggregateRoot<Guid>, ITenantOwned
{
    private OnboardingProcess(Guid id, string tenantId, Guid employeeId, string employeeName)
        : base(id)
    {
        TenantId = tenantId;
        EmployeeId = employeeId;
        EmployeeName = employeeName;
        Status = OnboardingStatus.NotStarted;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    private OnboardingProcess() { TenantId = string.Empty; EmployeeName = string.Empty; }

    public string TenantId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public string EmployeeName { get; private set; }
    public OnboardingStatus Status { get; private set; }

    public bool DocumentsVerified { get; private set; }
    public bool AssetsAssigned { get; private set; }
    public bool SystemAccountsCreated { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }

    public static OnboardingProcess Start(string tenantId, Guid employeeId, string employeeName)
    {
        return new OnboardingProcess(Guid.NewGuid(), tenantId, employeeId, employeeName);
    }

    public void VerifyDocuments()
    {
        DocumentsVerified = true;
        EvaluateStatus();
    }

    public void AssignAssets()
    {
        AssetsAssigned = true;
        EvaluateStatus();
    }

    public void ProvisionSystems()
    {
        SystemAccountsCreated = true;
        EvaluateStatus();
    }

    public void CompleteOnboarding()
    {
        if (Status == OnboardingStatus.Completed) return;

        Status = OnboardingStatus.Completed;
        CompletedAtUtc = DateTimeOffset.UtcNow;
    }

    private void EvaluateStatus()
    {
        if (Status == OnboardingStatus.NotStarted)
            Status = OnboardingStatus.InProcess;
    }
}
