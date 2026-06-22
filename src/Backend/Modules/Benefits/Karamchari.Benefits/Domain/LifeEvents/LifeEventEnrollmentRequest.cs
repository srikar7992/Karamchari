using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Benefits.Domain.LifeEvents;

public sealed class LifeEventEnrollmentRequest : AggregateRoot<Guid>, ITenantOwned
{
    private LifeEventEnrollmentRequest() { }

    public string TenantId { get; private set; } = string.Empty;
    public Guid EmployeeId { get; private set; }
    public LifeEventType EventType { get; private set; }
    public DateOnly EventDate { get; private set; }
    public DateOnly QleWindowExpiry => EventDate.AddDays(30);
    public string Status { get; private set; } = "Pending";

    public static LifeEventEnrollmentRequest Create(
        string tenantId, Guid employeeId, LifeEventType eventType, DateOnly eventDate)
        => new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EmployeeId = employeeId,
            EventType = eventType,
            EventDate = eventDate
        };

    public void Approve() => Status = "Approved";
    public void MarkExpired() => Status = "Expired";
}
