namespace Karamchari.Analytics.Domain;

public sealed class AggMonthlyHeadcount
{
    public string TenantId { get; init; } = string.Empty;
    public int Year { get; init; }
    public int Month { get; init; }
    public Guid DepartmentId { get; init; }
    public int HeadcountStart { get; set; }
    public int HeadcountEnd { get; set; }
    public int Hires { get; set; }
    public int Attrition { get; set; }
    public int NetChange => HeadcountEnd - HeadcountStart;
}
