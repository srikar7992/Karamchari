// -----------------------------------------------------------------------
// <copyright file="SalaryRevisionImportProcessor.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Multitenancy;
using Karamchari.DataMigration.Services;
using Karamchari.Payroll.Data;
using Karamchari.Payroll.Domain.SalaryRevisions;

namespace Karamchari.DataMigration.Features.SalaryRevision;

public sealed class SalaryRevisionImportProcessor : IImportProcessor<SalaryRevisionImportRecord>
{
    private readonly IReferenceResolver _resolver;
    private readonly PayrollDbContext _db;
    private readonly ITenantProvider _tenantProvider;

    public SalaryRevisionImportProcessor(IReferenceResolver resolver, PayrollDbContext db, ITenantProvider tenantProvider)
    {
        _resolver = resolver;
        _db = db;
        _tenantProvider = tenantProvider;
    }

    public string ImportType => "SalaryRevisionImport";

    public async Task ProcessBatchAsync(IEnumerable<SalaryRevisionImportRecord> items, CancellationToken ct = default)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();

        foreach (var item in items)
        {
            var empId = await _resolver.ResolveEmployeeIdAsync(item.EmployeeNumber!, ct);
            if (empId is null) continue;

            var empName = await _resolver.ResolveEmployeeNameAsync(empId.Value, ct) ?? item.EmployeeNumber!;
            var effectiveFrom = DateOnly.TryParse(item.EffectiveDate, out var d) ? d : DateOnly.FromDateTime(DateTime.Today);
            var revisionType = Enum.TryParse<RevisionType>(item.RevisionType, true, out var rt) ? rt : RevisionType.OffCycle;

            var revision = Karamchari.Payroll.Domain.SalaryRevisions.SalaryRevision.Create(
                tenantId: tenantId,
                employeeId: empId.Value,
                employeeName: empName,
                type: revisionType,
                previousCTC: item.PreviousCTC ?? 0m,
                newCTC: item.NewCTC!.Value,
                effectiveFrom: effectiveFrom,
                requiresArrears: false,
                initiatedBy: "BulkImport",
                remarks: item.Remarks ?? "Imported via bulk import");

            _db.SalaryRevisions.Add(revision);
        }

        await _db.SaveChangesAsync(ct);
    }
}
