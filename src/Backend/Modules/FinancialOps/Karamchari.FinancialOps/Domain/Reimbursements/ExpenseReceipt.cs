using Karamchari.Core.Domain.Primitives;

namespace Karamchari.FinancialOps.Domain.Reimbursements;

/// <summary>
/// Represents a receipt attached to an expense claim.
/// Ensures audit-safe storage and immutable references to physical documents.
/// </summary>
public sealed class ExpenseReceipt : Entity<Guid>
{
    private ExpenseReceipt(
        Guid id,
        string fileName,
        string contentType,
        string storagePath,
        string checksum,
        long sizeInBytes)
        : base(id)
    {
        FileName = fileName;
        ContentType = contentType;
        StoragePath = storagePath;
        Checksum = checksum;
        SizeInBytes = sizeInBytes;
        UploadedAt = DateTimeOffset.UtcNow;
    }

    private ExpenseReceipt()
    {
        // EF Core
        FileName = string.Empty;
        ContentType = string.Empty;
        StoragePath = string.Empty;
        Checksum = string.Empty;
    }

    /// <summary>
    /// Gets the original name of the uploaded file.
    /// </summary>
    public string FileName { get; private set; }

    /// <summary>
    /// Gets the MIME type of the file.
    /// </summary>
    public string ContentType { get; private set; }

    /// <summary>
    /// Gets the immutable path to the file in the audit-safe storage provider.
    /// </summary>
    public string StoragePath { get; private set; }

    /// <summary>
    /// Gets the SHA-256 checksum of the file for duplicate detection and integrity verification.
    /// </summary>
    public string Checksum { get; private set; }

    /// <summary>
    /// Gets the size of the file in bytes.
    /// </summary>
    public long SizeInBytes { get; private set; }

    /// <summary>
    /// Gets the timestamp when the receipt was uploaded.
    /// </summary>
    public DateTimeOffset UploadedAt { get; private set; }

    /// <summary>
    /// Attaches a new receipt to a claim.
    /// </summary>
    public static ExpenseReceipt Create(
        string fileName,
        string contentType,
        string storagePath,
        string checksum,
        long sizeInBytes)
    {
        return new ExpenseReceipt(
            Guid.NewGuid(),
            fileName,
            contentType,
            storagePath,
            checksum,
            sizeInBytes);
    }
}
