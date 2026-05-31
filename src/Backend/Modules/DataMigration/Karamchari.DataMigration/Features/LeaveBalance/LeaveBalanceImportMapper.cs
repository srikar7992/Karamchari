using Karamchari.DataMigration.Services;

namespace Karamchari.DataMigration.Features.LeaveBalance;

public sealed class LeaveBalanceImportMapper : IImportMapper<LeaveBalanceImportRecord>
{
    public LeaveBalanceImportRecord Map(ImportRow row) => new()
    {
        EmployeeNumber = row.Data.GetValueOrDefault("employee number") ?? row.Data.GetValueOrDefault("emp id"),
        LeaveTypeName = row.Data.GetValueOrDefault("leave type") ?? row.Data.GetValueOrDefault("policy"),
        Balance = decimal.TryParse(row.Data.GetValueOrDefault("balance"), out var b) ? b : null,
        EffectiveDate = row.Data.GetValueOrDefault("effective date") ?? row.Data.GetValueOrDefault("date")
    };
}
