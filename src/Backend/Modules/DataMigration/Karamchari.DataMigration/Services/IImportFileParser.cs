namespace Karamchari.DataMigration.Services;

/// <summary>
/// Represents a single raw row from an import file.
/// </summary>
/// <param name="RowNumber">The 1-based index of the row in the source file.</param>
/// <param name="Data">Dictionary of column names to string values.</param>
public record ImportRow(int RowNumber, IReadOnlyDictionary<string, string> Data);

/// <summary>
/// Service responsible for streaming rows from a supported file format.
/// </summary>
public interface IImportFileParser
{
    /// <summary>The file extension this parser handles (e.g., ".csv").</summary>
    string SupportedExtension { get; }

    /// <summary>
    /// Streams rows from the provided stream.
    /// </summary>
    /// <param name="stream">The source stream.</param>
    /// <returns>An async enumerable of rows.</returns>
    IAsyncEnumerable<ImportRow> ParseAsync(Stream stream);
}
