namespace Karamchari.Payroll.Domain.FnF;

public sealed class FnFLineItem
{
    public Guid Id { get; private set; }
    public FnFLineItemType Type { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public bool IsDeduction { get; private set; }
    public string? Reference { get; private set; }
    public bool IsTaxable { get; private set; }

    private FnFLineItem() { }

    public static FnFLineItem Create(
        FnFLineItemType type,
        string description,
        decimal amount,
        bool isDeduction,
        bool isTaxable = false,
        string? reference = null)
    {
        return new FnFLineItem
        {
            Id = Guid.NewGuid(),
            Type = type,
            Description = description,
            Amount = Math.Abs(amount),
            IsDeduction = isDeduction,
            IsTaxable = isTaxable,
            Reference = reference
        };
    }

    public decimal SignedAmount => IsDeduction ? -Amount : Amount;
}
