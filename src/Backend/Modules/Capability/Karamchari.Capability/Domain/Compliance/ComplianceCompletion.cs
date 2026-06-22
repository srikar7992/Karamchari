using Karamchari.Core.Multitenancy;

namespace Karamchari.Capability.Domain.Compliance;

public sealed class ComplianceCompletion : ITenantOwned
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string TenantId { get; init; } = string.Empty;
    public Guid AssignmentId { get; init; }
    public Guid EmployeeId { get; init; }
    public DateTimeOffset CompletedAtUtc { get; init; }
    public int? Score { get; init; }
    public string? IdempotencyKey { get; init; }
}
