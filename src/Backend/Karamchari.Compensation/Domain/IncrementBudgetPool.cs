using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Compensation.Domain;

/// <summary>
/// Department-level increment budget pool for a merit cycle.
/// Total budget is allocated top-down; managers draw from their pool when recommending increments.
/// HR approves the pool before managers can allocate.
///
/// Budget safety: allocated amount is tracked in real-time. Managers cannot exceed their pool.
/// Any remaining budget at close is returned to the HR pool for redistribution.
/// </summary>
public sealed class IncrementBudgetPool : AggregateRoot<Guid>, ITenantOwned
{
    private IncrementBudgetPool() { /* EF materialization */ }

    private IncrementBudgetPool(
        Guid id,
        string tenantId,
        Guid departmentId,
        string departmentName,
        int reviewYear,
        decimal totalBudget,
        string currencyCode) : base(id)
    {
        TenantId = tenantId;
        DepartmentId = departmentId;
        DepartmentName = departmentName;
        ReviewYear = reviewYear;
        TotalBudget = totalBudget;
        AllocatedBudget = 0m;
        CurrencyCode = currencyCode;
        Status = BudgetStatus.Draft;
    }

    public string TenantId { get; private set; } = string.Empty;
    public Guid DepartmentId { get; private set; }
    public string DepartmentName { get; private set; } = string.Empty;
    public int ReviewYear { get; private set; }
    public decimal TotalBudget { get; private set; }
    public decimal AllocatedBudget { get; private set; }
    public decimal RemainingBudget => TotalBudget - AllocatedBudget;
    public string CurrencyCode { get; private set; } = string.Empty;
    public BudgetStatus Status { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public static IncrementBudgetPool Create(
        string tenantId,
        Guid departmentId,
        string departmentName,
        int reviewYear,
        decimal totalBudget,
        string currencyCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(departmentName);
        ArgumentException.ThrowIfNullOrWhiteSpace(currencyCode);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(totalBudget);
        if (reviewYear < 2020 || reviewYear > 2100) throw new ArgumentOutOfRangeException(nameof(reviewYear));

        return new IncrementBudgetPool(
            Guid.NewGuid(), tenantId, departmentId, departmentName.Trim(),
            reviewYear, totalBudget, currencyCode.ToUpperInvariant());
    }

    public void Approve()
    {
        if (Status != BudgetStatus.Draft)
            throw new InvalidOperationException($"Cannot approve pool in state {Status}.");
        Status = BudgetStatus.Approved;
    }

    public void Allocate(decimal amount)
    {
        if (Status != BudgetStatus.Approved)
            throw new InvalidOperationException("Pool must be approved before allocation.");
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);
        if (amount > RemainingBudget)
            throw new InvalidOperationException(
                $"Allocation of {amount} {CurrencyCode} exceeds remaining budget of {RemainingBudget} {CurrencyCode}.");

        AllocatedBudget += amount;
        Status = BudgetStatus.Allocated;
    }

    public void Close()
    {
        if (Status is BudgetStatus.Draft)
            throw new InvalidOperationException("Cannot close a pool that was never approved.");
        Status = BudgetStatus.Closed;
    }
}
