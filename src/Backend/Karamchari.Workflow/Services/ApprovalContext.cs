namespace Karamchari.Workflow.Services;

public sealed class ApprovalContext
{
    public decimal Amount { get; init; }
    public string ProjectType { get; init; } = string.Empty;
    public bool IsBillable { get; init; }
    public string Department { get; init; } = string.Empty;
    public string EntityType { get; init; } = string.Empty;
    public string TenantId { get; init; } = string.Empty;

    public static readonly ApprovalContext Empty = new();
}
