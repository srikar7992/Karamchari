namespace Karamchari.Payroll.Domain;

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

/// <summary>
/// Defines the recurring payroll cycle for a tenant (e.g., Monthly on the 1st).
/// </summary>
public sealed class PayrollSchedule : AggregateRoot<Guid>, ITenantOwned
{
    private PayrollSchedule(Guid id, string name, int dayOfMonth) : base(id)
    {
        Name = name;
        DayOfMonth = dayOfMonth;
    }

    private PayrollSchedule()
    {
        TenantId = string.Empty;
        Name = string.Empty;
    }

    /// <summary>
    /// Gets the tenant identifier.
    /// </summary>
    public string TenantId { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the name of the schedule (e.g., "Monthly Standard").
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Gets the day of the month on which payroll is processed.
    /// </summary>
    public int DayOfMonth { get; private set; }

    /// <summary>
    /// Creates a new payroll schedule.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="dayOfMonth">The day of the month (1-31).</param>
    /// <returns>A new <see cref="PayrollSchedule"/> instance.</returns>
    public static PayrollSchedule Create(string name, int dayOfMonth)
    {
        if (dayOfMonth < 1 || dayOfMonth > 31) throw new ArgumentOutOfRangeException(nameof(dayOfMonth));
        return new PayrollSchedule(Guid.NewGuid(), name, dayOfMonth);
    }
}
