using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Compensation.Domain;

public enum BonusPoolStatus { Draft, Approved, Allocated, Closed }

public sealed class BonusPool : AggregateRoot<Guid>, ITenantOwned
{
    private BonusPool() { }
    private BonusPool(Guid id, string tenantId, Guid cycleId, string poolName,
        decimal totalBudget, string currencyCode) : base(id)
    {
        TenantId = tenantId;
        CycleId = cycleId;
        PoolName = poolName;
        TotalBudget = totalBudget;
        AllocatedAmount = 0m;
        CurrencyCode = currencyCode;
        Status = BonusPoolStatus.Draft;
    }

    public string TenantId { get; private set; } = string.Empty;
    public Guid CycleId { get; private set; }
    public string PoolName { get; private set; } = string.Empty;
    public decimal TotalBudget { get; private set; }
    public decimal AllocatedAmount { get; private set; }
    public decimal RemainingBudget => TotalBudget - AllocatedAmount;
    public string CurrencyCode { get; private set; } = string.Empty;
    public BonusPoolStatus Status { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public static BonusPool Create(string tenantId, Guid cycleId, string poolName,
        decimal totalBudget, string currencyCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(poolName);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(totalBudget);
        return new BonusPool(Guid.NewGuid(), tenantId, cycleId, poolName, totalBudget, currencyCode.ToUpperInvariant());
    }

    public void Approve()
    {
        if (Status != BonusPoolStatus.Draft) throw new InvalidOperationException($"Cannot approve in state {Status}.");
        Status = BonusPoolStatus.Approved;
    }

    public void Allocate(decimal amount)
    {
        if (Status != BonusPoolStatus.Approved) throw new InvalidOperationException("Pool must be approved before allocation.");
        if (amount > RemainingBudget) throw new InvalidOperationException($"Allocation {amount} exceeds remaining {RemainingBudget}.");
        AllocatedAmount += amount;
        Status = BonusPoolStatus.Allocated;
    }

    public void Close() => Status = BonusPoolStatus.Closed;
}
