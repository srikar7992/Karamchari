namespace Karamchari.Payroll.Services.Payslip;

using Microsoft.Extensions.Hosting;

/// <summary>
/// A local file system implementation of payslip storage.
/// Useful for development and on-premise deployments.
/// </summary>
public sealed class LocalFilePayslipStorage : IPayslipStorage
{
    private readonly string _basePath;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalFilePayslipStorage"/> class.
    /// </summary>
    public LocalFilePayslipStorage()
    {
        // For development, we store in the project root's artifacts directory
        _basePath = Path.Combine(Directory.GetCurrentDirectory(), "artifacts", "payslips");
        
        if (!Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
        }
    }

    /// <inheritdoc/>
    public async Task<string> SaveAsync(string employeeId, string periodName, byte[] pdfBytes)
    {
        ArgumentNullException.ThrowIfNull(periodName);

        string directory = Path.Combine(_basePath, employeeId);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string fileName = $"{periodName.Replace(" ", "_")}.pdf";
        string filePath = Path.Combine(directory, fileName);

        await File.WriteAllBytesAsync(filePath, pdfBytes);
        return filePath;
    }

    /// <inheritdoc/>
    public async Task<byte[]> GetAsync(string employeeId, string periodName)
    {
        ArgumentNullException.ThrowIfNull(periodName);

        string fileName = $"{periodName.Replace(" ", "_")}.pdf";
        string filePath = Path.Combine(_basePath, employeeId, fileName);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Payslip for {periodName} not found.", filePath);
        }

        return await File.ReadAllBytesAsync(filePath);
    }
}
