using Karamchari.Forecasting.Domain;

namespace Karamchari.Forecasting.Services;

public static class ScenarioModelingEngine
{
    public static IReadOnlyList<MonthlyWorkforceProjection> Project(
        int startingHeadcount, decimal avgMonthlySalary,
        IEnumerable<ScenarioAssumption> assumptions, int horizonMonths = 36)
    {
        var assumptionList = assumptions.ToList();
        var attritionRate = assumptionList.FirstOrDefault(a => a.Type == AssumptionType.AttritionRate)?.Value ?? 0.10m;
        var hiresPerMonth = assumptionList.FirstOrDefault(a => a.Type == AssumptionType.HiringVelocity)?.Value ?? 5m;
        var salaryInflation = assumptionList.FirstOrDefault(a => a.Type == AssumptionType.SalaryInflation)?.Value ?? 0.07m;

        var result = new List<MonthlyWorkforceProjection>(horizonMonths);
        var hc = (decimal)startingHeadcount;
        var salary = avgMonthlySalary;

        for (int m = 1; m <= horizonMonths; m++)
        {
            var monthlyAttrition = (int)Math.Round(hc * (attritionRate / 12));
            var monthlyHires = (int)Math.Round(hiresPerMonth);
            hc = Math.Max(0, hc - monthlyAttrition + monthlyHires);
            if (m % 12 == 0) salary *= (1 + salaryInflation);
            result.Add(new MonthlyWorkforceProjection(m, (int)hc, hc * salary, monthlyHires, monthlyAttrition));
        }
        return result;
    }
}
