using System.Globalization;

namespace Karamchari.Payroll.Services.Declarations;

public sealed class LocalProofStorage : IProofStorage
{
    private readonly string _basePath;

    public LocalProofStorage()
    {
        _basePath = Path.Combine(Directory.GetCurrentDirectory(), "artifacts", "tax-proofs");
        if (!Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
        }
    }

    public async Task<string> SaveAsync(Stream stream, string fileName, string tenantId, Guid employeeId, int financialYear)
    {
        ArgumentNullException.ThrowIfNull(stream);
        string directory = Path.Combine(_basePath, tenantId, financialYear.ToString(CultureInfo.InvariantCulture), employeeId.ToString());
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string fileId = Guid.NewGuid().ToString("N");
        string extension = Path.GetExtension(fileName);
        string physicalPath = Path.Combine(directory, $"{fileId}{extension}");

        using var fileStream = File.Create(physicalPath);
        await stream.CopyToAsync(fileStream);

        return physicalPath;
    }

    public Task<Stream> GetStreamAsync(string proofUri)
    {
        if (!File.Exists(proofUri))
        {
            throw new FileNotFoundException("Proof document not found.");
        }

        return Task.FromResult<Stream>(File.OpenRead(proofUri));
    }
}
