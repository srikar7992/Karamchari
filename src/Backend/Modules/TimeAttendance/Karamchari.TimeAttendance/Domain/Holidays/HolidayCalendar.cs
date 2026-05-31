namespace Karamchari.TimeAttendance.Domain.Holidays;

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

/// <summary>
/// Represents a regional or organizational holiday calendar.
/// </summary>
public sealed class HolidayCalendar : AggregateRoot<Guid>, ITenantOwned
{
    private readonly List<Holiday> _holidays = new();

    private HolidayCalendar(Guid id, string name, string? description) : base(id)
    {
        Name = name;
        Description = description;
    }

    private HolidayCalendar()
    {
        Name = string.Empty;
        TenantId = string.Empty;
    }

    /// <summary>
    /// Gets the tenant identifier.
    /// </summary>
    public string TenantId { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the name of the calendar.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Gets the optional description of the calendar.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Gets the list of holidays in this calendar.
    /// </summary>
    public IReadOnlyCollection<Holiday> Holidays => _holidays.AsReadOnly();

    /// <summary>
    /// Creates a new holiday calendar.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="description">The description.</param>
    /// <returns>A new <see cref="HolidayCalendar"/> instance.</returns>
    public static HolidayCalendar Create(string name, string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new HolidayCalendar(Guid.NewGuid(), name.Trim(), description?.Trim());
    }

    /// <summary>
    /// Adds a holiday to the calendar.
    /// </summary>
    /// <param name="date">The date.</param>
    /// <param name="name">The name of the holiday.</param>
    public void AddHoliday(DateOnly date, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (_holidays.Any(h => h.Date == date))
        {
            throw new InvalidOperationException($"A holiday already exists on {date}.");
        }
        _holidays.Add(new Holiday(Guid.NewGuid(), date, name.Trim()));
    }
}
