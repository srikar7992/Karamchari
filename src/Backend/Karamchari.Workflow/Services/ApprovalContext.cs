namespace Karamchari.Workflow.Services;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed class ApprovalContext
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal Amount { get; init; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string ProjectType { get; init; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public bool IsBillable { get; init; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string Department { get; init; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string EntityType { get; init; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TenantId { get; init; } = string.Empty;

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static readonly ApprovalContext Empty = new();
}
