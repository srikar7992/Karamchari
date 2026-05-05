namespace Karamchari.PSA.Domain;

using Karamchari.Core.Multitenancy;

/// <summary>
/// Records a single day's worth of billable work in the unbilled revenue ledger.
/// Rows are created by <c>BillableRevenueConsumer</c> when a timesheet is approved.
/// Rows are marked <see cref="IsInvoiced"/> when included in a generated invoice.
/// </summary>
public sealed class UnbilledRevenue : ITenantOwned
{
    /// <inheritdoc/>
    public string TenantId { get; private set; } = string.Empty;

    /// <summary>Gets the unique identifier.</summary>
    public Guid Id { get; private set; }

    /// <summary>Gets the employee who performed the work.</summary>
    public Guid EmployeeId { get; private set; }

    /// <summary>Gets the project the work was logged against.</summary>
    public Guid ProjectId { get; private set; }

    /// <summary>Gets the source timesheet identifier for traceability.</summary>
    public Guid TimesheetId { get; private set; }

    /// <summary>Gets the number of billable hours.</summary>
    public decimal BillableHours { get; private set; }

    /// <summary>Gets the rate per hour that was active on the work date.</summary>
    public decimal BillableRate { get; private set; }

    /// <summary>Gets the computed revenue amount (<c>BillableHours * BillableRate</c>).</summary>
    public decimal Amount { get; private set; }

    /// <summary>Gets the currency of the amount.</summary>
    public string Currency { get; private set; } = "INR";

    /// <summary>Gets the date on which the work was performed.</summary>
    public DateOnly WorkDate { get; private set; }

    /// <summary>Gets whether this entry has already been included in an invoice.</summary>
    public bool IsInvoiced { get; private set; }

    /// <summary>Gets the invoice identifier once this entry is invoiced.</summary>
    public Guid? InvoiceId { get; private set; }

    private UnbilledRevenue() { }

    /// <summary>
    /// Creates a new revenue entry from approved timesheet work.
    /// </summary>
    public static UnbilledRevenue Record(
        Guid timesheetId,
        Guid employeeId,
        Guid projectId,
        decimal billableHours,
        decimal billableRate,
        string currency,
        DateOnly workDate)
    {
        if (billableHours <= 0) throw new ArgumentOutOfRangeException(nameof(billableHours));
        if (billableRate < 0) throw new ArgumentOutOfRangeException(nameof(billableRate));

        return new UnbilledRevenue
        {
            Id = Guid.NewGuid(),
            TimesheetId = timesheetId,
            EmployeeId = employeeId,
            ProjectId = projectId,
            BillableHours = billableHours,
            BillableRate = billableRate,
            Amount = Math.Round(billableHours * billableRate, 2),
            Currency = currency.ToUpperInvariant(),
            WorkDate = workDate,
            IsInvoiced = false
        };
    }

    /// <summary>
    /// Marks this entry as included in the given invoice.
    /// Once invoiced, it must NOT be included in any future invoices.
    /// </summary>
    public void MarkAsInvoiced(Guid invoiceId)
    {
        if (IsInvoiced)
            throw new InvalidOperationException($"Revenue entry {Id} is already invoiced under {InvoiceId}.");

        IsInvoiced = true;
        InvoiceId = invoiceId;
    }
}
