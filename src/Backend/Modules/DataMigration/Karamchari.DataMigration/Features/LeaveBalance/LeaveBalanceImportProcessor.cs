// -----------------------------------------------------------------------
// <copyright file="LeaveBalanceImportProcessor.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.DataMigration.Services;
using Karamchari.TimeAttendance.Persistence;
using TaLeaveBalance = Karamchari.TimeAttendance.Domain.Leaves.LeaveBalance;

namespace Karamchari.DataMigration.Features.LeaveBalance;

public sealed class LeaveBalanceImportProcessor : IImportProcessor<LeaveBalanceImportRecord>
{
    private readonly IReferenceResolver _resolver;
    private readonly TimeAttendanceDbContext _db;

    public LeaveBalanceImportProcessor(IReferenceResolver resolver, TimeAttendanceDbContext db)
    {
        _resolver = resolver;
        _db = db;
    }

    public string ImportType => "LeaveBalanceImport";

    public async Task ProcessBatchAsync(IEnumerable<LeaveBalanceImportRecord> items, CancellationToken ct = default)
    {
        foreach (var item in items)
        {
            var empId = await _resolver.ResolveEmployeeIdAsync(item.EmployeeNumber!, ct);
            var policyId = await _resolver.ResolveLeavePolicyIdAsync(item.LeaveTypeName!, ct);

            if (empId is null || policyId is null) continue;

            var effectiveDate = DateOnly.TryParse(item.EffectiveDate, out var d)
                ? d
                : DateOnly.FromDateTime(DateTime.Today);

            var balance = TaLeaveBalance.Create(empId.Value, policyId.Value);
            balance.Accrue(item.Balance!.Value, effectiveDate, remarks: "Bulk import", correlationId: "BulkImport");

            _db.LeaveBalances.Add(balance);
        }

        await _db.SaveChangesAsync(ct);
    }
}
