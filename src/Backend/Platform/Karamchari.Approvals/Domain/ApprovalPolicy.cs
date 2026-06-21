using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Approvals.Domain;

public sealed class ApprovalPolicy : AggregateRoot<Guid>, ITenantOwned
{
    private readonly List<ApprovalPolicyStep> _steps = [];

    private ApprovalPolicy() { }

    public string TenantId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public ApprovalRequestType RequestType { get; private set; }
    public ApprovalPolicyMode Mode { get; private set; }
    public bool IsDefault { get; private set; }
    public IReadOnlyList<ApprovalPolicyStep> Steps => _steps.AsReadOnly();

    public static ApprovalPolicy Create(
        string tenantId,
        string name,
        ApprovalRequestType requestType,
        ApprovalPolicyMode mode,
        bool isDefault = false)
        => new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            RequestType = requestType,
            Mode = mode,
            IsDefault = isDefault
        };

    public void AddStep(ApprovalPolicyStep step) => _steps.Add(step);
}
