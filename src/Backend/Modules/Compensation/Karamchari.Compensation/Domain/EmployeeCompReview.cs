using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Compensation.Domain;

public enum ReviewStatus { Pending, Recommended, Approved, Rejected }

public sealed class EmployeeCompReview : AggregateRoot<Guid>, ITenantOwned
{
    private EmployeeCompReview() { }
    private EmployeeCompReview(Guid id, string tenantId, Guid cycleId, Guid employeeId,
        decimal currentCTC, string currencyCode) : base(id)
    {
        TenantId = tenantId;
        CycleId = cycleId;
        EmployeeId = employeeId;
        CurrentCTC = currentCTC;
        CurrencyCode = currencyCode;
        Status = ReviewStatus.Pending;
    }

    public string TenantId { get; private set; } = string.Empty;
    public Guid CycleId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public decimal CurrentCTC { get; private set; }
    public decimal? RecommendedCTC { get; private set; }
    public decimal? IncrementPercent { get; private set; }
    public string? ManagerJustification { get; private set; }
    public string? RejectionReason { get; private set; }
    public string CurrencyCode { get; private set; } = string.Empty;
    public ReviewStatus Status { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public static EmployeeCompReview Create(string tenantId, Guid cycleId, Guid employeeId,
        decimal currentCTC, string currencyCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        return new EmployeeCompReview(Guid.NewGuid(), tenantId, cycleId, employeeId, currentCTC, currencyCode);
    }

    public void Recommend(decimal newCTC, string justification)
    {
        if (Status != ReviewStatus.Pending) throw new InvalidOperationException("Review already acted on.");
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(newCTC);
        RecommendedCTC = newCTC;
        IncrementPercent = CurrentCTC > 0 ? Math.Round((newCTC - CurrentCTC) / CurrentCTC * 100, 2) : 0;
        ManagerJustification = justification;
        Status = ReviewStatus.Recommended;
    }

    public void Approve()
    {
        if (Status != ReviewStatus.Recommended) throw new InvalidOperationException("Can only approve a recommended review.");
        Status = ReviewStatus.Approved;
    }

    public void Reject(string reason)
    {
        if (Status == ReviewStatus.Approved) throw new InvalidOperationException("Cannot reject an approved review.");
        RejectionReason = reason;
        Status = ReviewStatus.Rejected;
    }
}
