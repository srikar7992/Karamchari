// -----------------------------------------------------------------------
// <copyright file="LocalFilePayslipStorage.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Payroll.Services.Payslip;

using Karamchari.Core.Multitenancy;

/// <summary>
/// A local file system implementation of payslip storage.
/// Useful for development and on-premise deployments.
/// </summary>
/// <remarks>
/// File layout: <c>artifacts/payslips/{tenantId}/{employeeId}/{period}.pdf</c>
/// The tenant segment ensures payslips from different tenants never share a directory,
/// preventing cross-tenant file collisions and making per-tenant cleanup straightforward.
/// The production implementation should use Azure Blob Storage with tenant-scoped containers.
/// </remarks>
public sealed class LocalFilePayslipStorage : IPayslipStorage
{
    private readonly string _basePath;
    private readonly ITenantProvider _tenantProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalFilePayslipStorage"/> class.
    /// </summary>
    public LocalFilePayslipStorage(ITenantProvider tenantProvider)
    {
        _tenantProvider = tenantProvider ?? throw new ArgumentNullException(nameof(tenantProvider));
        _basePath = Path.Combine(Directory.GetCurrentDirectory(), "artifacts", "payslips");

        if (!Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
        }
    }

    private string BuildFilePath(string employeeId, string periodName)
    {
        // Scope the path by tenant to enforce isolation between tenants on the local filesystem.
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var safePeriod = periodName.Replace(" ", "_");
        return Path.Combine(_basePath, tenantId, employeeId, $"{safePeriod}.pdf");
    }

    /// <inheritdoc/>
    public async Task<string> SaveAsync(string employeeId, string periodName, byte[] pdfBytes)
    {
        ArgumentNullException.ThrowIfNull(periodName);

        var filePath = BuildFilePath(employeeId, periodName);
        var directory = Path.GetDirectoryName(filePath)!;

        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllBytesAsync(filePath, pdfBytes);
        return filePath;
    }

    /// <inheritdoc/>
    public async Task<byte[]> GetAsync(string employeeId, string periodName)
    {
        ArgumentNullException.ThrowIfNull(periodName);

        var filePath = BuildFilePath(employeeId, periodName);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Payslip for {periodName} not found.", filePath);
        }

        return await File.ReadAllBytesAsync(filePath);
    }

    /// <inheritdoc/>
    public Task<bool> ExistsAsync(string employeeId, string periodName)
    {
        ArgumentNullException.ThrowIfNull(periodName);
        var filePath = BuildFilePath(employeeId, periodName);
        return Task.FromResult(File.Exists(filePath));
    }
}
