using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.AssetManagement.Domain;

public enum AssetStatus { Available, Assigned, UnderMaintenance, Retired, Lost }
public enum AssetCondition { New, Good, Fair, Poor }

public record AssetId(Guid Value)
{
    public static AssetId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

public record AssetCategoryId(Guid Value)
{
    public static AssetCategoryId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Organizational asset. Tracks inventory, condition, assignments, and offboarding returns.
/// Single assignment active at a time — enforced by domain methods.
/// </summary>
public sealed class Asset : AggregateRoot<AssetId>, ITenantOwned
{
    private readonly List<AssetAssignment> _assignments = [];

    private Asset(
        AssetId id,
        string tenantId,
        AssetCategoryId categoryId,
        string name,
        string? serialNumber,
        string? assetTag,
        decimal purchaseValue,
        DateOnly purchasedOn,
        AssetCondition condition)
        : base(id)
    {
        TenantId = tenantId;
        CategoryId = categoryId;
        Name = name;
        SerialNumber = serialNumber;
        AssetTag = assetTag;
        PurchaseValue = purchaseValue;
        PurchasedOn = purchasedOn;
        Condition = condition;
        Status = AssetStatus.Available;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    private Asset() { TenantId = string.Empty; Name = string.Empty; }

    public string TenantId { get; private set; }
    public AssetCategoryId CategoryId { get; private set; } = null!;
    public string Name { get; private set; }
    public string? SerialNumber { get; private set; }
    public string? AssetTag { get; private set; }
    public decimal PurchaseValue { get; private set; }
    public DateOnly PurchasedOn { get; private set; }
    public AssetCondition Condition { get; private set; }
    public AssetStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public IReadOnlyList<AssetAssignment> Assignments => _assignments.AsReadOnly();

    public AssetAssignment? CurrentAssignment =>
        _assignments.SingleOrDefault(a => a.ReturnedOn == null);

    public static Asset Register(
        string tenantId,
        AssetCategoryId categoryId,
        string name,
        string? serialNumber,
        string? assetTag,
        decimal purchaseValue,
        DateOnly purchasedOn,
        AssetCondition condition = AssetCondition.New)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new Asset(AssetId.New(), tenantId, categoryId, name, serialNumber, assetTag, purchaseValue, purchasedOn, condition);
    }

    public void AssignTo(Guid employeeId, DateOnly assignedOn, string? notes = null)
    {
        if (Status == AssetStatus.Assigned)
            throw new InvalidOperationException($"Asset already assigned to employee {CurrentAssignment!.EmployeeId}.");
        if (Status == AssetStatus.Retired || Status == AssetStatus.Lost)
            throw new InvalidOperationException($"Cannot assign asset in state {Status}.");

        _assignments.Add(new AssetAssignment(Guid.NewGuid(), Id, employeeId, assignedOn, notes));
        Status = AssetStatus.Assigned;
    }

    public void RecordReturn(DateOnly returnedOn, AssetCondition conditionOnReturn, string? notes = null)
    {
        var active = CurrentAssignment
            ?? throw new InvalidOperationException("No active assignment to return.");

        active.RecordReturn(returnedOn, conditionOnReturn, notes);
        Condition = conditionOnReturn;
        Status = AssetStatus.Available;
    }

    public void Retire(string reason)
    {
        if (Status == AssetStatus.Assigned)
            throw new InvalidOperationException("Return asset before retiring.");
        Status = AssetStatus.Retired;
    }

    public void MarkLost() => Status = AssetStatus.Lost;

    public void UpdateCondition(AssetCondition condition) => Condition = condition;

    public void SendForMaintenance()
    {
        if (Status == AssetStatus.Assigned)
            throw new InvalidOperationException("Return asset before sending for maintenance.");
        Status = AssetStatus.UnderMaintenance;
    }

    public void ReturnFromMaintenance(AssetCondition conditionAfter)
    {
        if (Status != AssetStatus.UnderMaintenance)
            throw new InvalidOperationException("Asset is not under maintenance.");
        Condition = conditionAfter;
        Status = AssetStatus.Available;
    }
}

/// <summary>
/// Tracks one employee-asset assignment lifecycle.
/// </summary>
public sealed class AssetAssignment : Entity<Guid>
{
    internal AssetAssignment(Guid id, AssetId assetId, Guid employeeId, DateOnly assignedOn, string? notes)
        : base(id)
    {
        AssetId = assetId;
        EmployeeId = employeeId;
        AssignedOn = assignedOn;
        Notes = notes;
    }

    private AssetAssignment() { AssetId = null!; }

    public AssetId AssetId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public DateOnly AssignedOn { get; private set; }
    public DateOnly? ReturnedOn { get; private set; }
    public AssetCondition? ConditionOnReturn { get; private set; }
    public string? Notes { get; private set; }

    internal void RecordReturn(DateOnly returnedOn, AssetCondition condition, string? notes)
    {
        ReturnedOn = returnedOn;
        ConditionOnReturn = condition;
        if (notes != null) Notes = notes;
    }
}

/// <summary>
/// Asset category master (Laptop, Phone, Access Card, Furniture, etc.)
/// </summary>
public sealed class AssetCategory : AggregateRoot<AssetCategoryId>, ITenantOwned
{
    private AssetCategory(AssetCategoryId id, string tenantId, string name, bool requiresReturn)
        : base(id)
    {
        TenantId = tenantId;
        Name = name;
        RequiresReturnOnOffboarding = requiresReturn;
        IsActive = true;
    }

    private AssetCategory() { TenantId = string.Empty; Name = string.Empty; }

    public string TenantId { get; private set; }
    public string Name { get; private set; }
    public bool RequiresReturnOnOffboarding { get; private set; }
    public bool IsActive { get; private set; }

    public static AssetCategory Create(string tenantId, string name, bool requiresReturn = true) =>
        new(AssetCategoryId.New(), tenantId, name, requiresReturn);

    public void Deactivate() => IsActive = false;
}
