using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Compensation.Domain;

public enum CompCycleStatus { Draft, Active, Closed }

public sealed class CompensationCycle : AggregateRoot<Guid>, ITenantOwned
{
    private CompensationCycle() { }
    private CompensationCycle(Guid id, string tenantId, string name, int year,
        DateOnly nominationOpen, DateOnly nominationClose, DateOnly effectiveDate) : base(id)
    {
        TenantId = tenantId;
        Name = name;
        Year = year;
        NominationOpenDate = nominationOpen;
        NominationCloseDate = nominationClose;
        EffectiveDate = effectiveDate;
        Status = CompCycleStatus.Draft;
    }

    public string TenantId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public int Year { get; private set; }
    public CompCycleStatus Status { get; private set; }
    public DateOnly NominationOpenDate { get; private set; }
    public DateOnly NominationCloseDate { get; private set; }
    public DateOnly EffectiveDate { get; private set; }
    public Guid? MeritMatrixId { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public static CompensationCycle Create(string tenantId, string name, int year,
        DateOnly nominationOpen, DateOnly nominationClose, DateOnly effectiveDate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (year < 2020 || year > 2100) throw new ArgumentOutOfRangeException(nameof(year));
        return new CompensationCycle(Guid.NewGuid(), tenantId, name, year, nominationOpen, nominationClose, effectiveDate);
    }

    public void LinkMatrix(Guid matrixId) => MeritMatrixId = matrixId;

    public void Open()
    {
        if (Status != CompCycleStatus.Draft) throw new InvalidOperationException($"Cannot open cycle in state {Status}.");
        Status = CompCycleStatus.Active;
    }

    public void Close()
    {
        if (Status == CompCycleStatus.Closed) throw new InvalidOperationException("Already closed.");
        Status = CompCycleStatus.Closed;
    }
}
