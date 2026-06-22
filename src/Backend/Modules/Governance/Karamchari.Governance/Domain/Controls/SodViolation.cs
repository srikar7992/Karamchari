namespace Karamchari.Governance.Domain.Controls;

public sealed class SodViolation
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string TenantId { get; init; } = string.Empty;
    public Guid EmployeeId { get; init; }
    public string Role1 { get; init; } = string.Empty;
    public string Role2 { get; init; } = string.Empty;
    public DateTimeOffset DetectedAt { get; init; } = DateTimeOffset.UtcNow;
}
