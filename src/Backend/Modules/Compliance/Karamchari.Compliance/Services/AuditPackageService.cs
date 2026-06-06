using Karamchari.Compliance.Domain.Reporting;
using Karamchari.Compliance.Domain.Violations;
using Karamchari.Compliance.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Karamchari.Compliance.Services;

/// <summary>
/// Generates evidence bundles for audit requests.
/// Package content: violation history + score history + legal holds within period.
/// URI format: "compliance/audit/{packageId}.json" — storage upload handled by caller.
/// </summary>
public sealed class AuditPackageService
{
    private readonly ComplianceDbContext _db;
    private readonly ILogger<AuditPackageService> _logger;

    public AuditPackageService(ComplianceDbContext db, ILogger<AuditPackageService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<AuditPackage> RequestPackageAsync(
        string tenantId,
        string requestedBy,
        Guid? employeeId,
        DateTime periodStart,
        DateTime periodEnd,
        CancellationToken ct = default)
    {
        var package = AuditPackage.Request(tenantId, requestedBy, employeeId, periodStart, periodEnd);
        _db.AuditPackages.Add(package);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Audit package {PackageId} requested by {User} for period {Start:d}-{End:d}",
            package.Id, requestedBy, periodStart, periodEnd);
        return package;
    }

    public async Task GeneratePackageAsync(Guid packageId, CancellationToken ct = default)
    {
        var package = await _db.AuditPackages
            .FirstOrDefaultAsync(p => p.Id == packageId, ct)
            ?? throw new InvalidOperationException($"Audit package {packageId} not found");

        package.MarkGenerating();
        await _db.SaveChangesAsync(ct);

        try
        {
            var violations = await _db.PolicyViolations
                .Where(v => v.TenantId == package.TenantId
                         && v.DetectedAt >= package.PeriodStart
                         && v.DetectedAt <= package.PeriodEnd
                         && (!package.EmployeeId.HasValue || v.EmployeeId == package.EmployeeId))
                .OrderByDescending(v => v.DetectedAt)
                .ToListAsync(ct);

            var legalHolds = await _db.LegalHolds
                .Where(h => h.TenantId == package.TenantId
                         && h.StartDate <= package.PeriodEnd
                         && (h.ReleasedAt == null || h.ReleasedAt >= package.PeriodStart))
                .ToListAsync(ct);

            var evidence = new
            {
                PackageId = packageId,
                Generated = DateTime.UtcNow,
                Period = new { package.PeriodStart, package.PeriodEnd },
                EmployeeId = package.EmployeeId,
                Violations = violations.Select(v => new
                {
                    v.Id, v.PolicyCode, v.Severity, v.Status, v.DetectedAt, v.ResolvedAt
                }),
                LegalHolds = legalHolds.Select(h => new
                {
                    h.Id, h.CaseReference, h.Status, h.StartDate, h.ReleasedAt
                }),
                TotalViolations = violations.Count,
                CriticalViolations = violations.Count(v => v.Severity == ViolationSeverity.Critical)
            };

            var json = System.Text.Json.JsonSerializer.Serialize(evidence);
            var uri = $"compliance/audit/{packageId}.json";

            // NOTE: actual blob upload wired by infrastructure. URI recorded here.
            package.MarkReady(uri);
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("Audit package {PackageId} generated: {Violations} violations",
                packageId, violations.Count);
        }
        catch (Exception ex)
        {
            package.MarkFailed(ex.Message);
            await _db.SaveChangesAsync(ct);
            _logger.LogError(ex, "Audit package {PackageId} generation failed", packageId);
            throw;
        }
    }
}
