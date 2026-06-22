using Karamchari.Benefits.Domain.Events;
using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Benefits.Domain.Enrollment;

public sealed class EnrollmentWindow : AggregateRoot<Guid>, ITenantOwned
{
    private EnrollmentWindow() { }

    public string TenantId { get; private set; } = string.Empty;
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public EnrollmentWindowStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static EnrollmentWindow Create(string tenantId, DateOnly start, DateOnly end)
    {
        if (end < start) throw new ArgumentException("EndDate must be >= StartDate.");
        return new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            StartDate = start,
            EndDate = end,
            Status = EnrollmentWindowStatus.Draft,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
    }

    public void Open()
    {
        Status = EnrollmentWindowStatus.Open;
        RaiseDomainEvent(new EnrollmentWindowOpened(Id, TenantId, StartDate, EndDate));
    }

    public void Close() => Status = EnrollmentWindowStatus.Closed;
}
