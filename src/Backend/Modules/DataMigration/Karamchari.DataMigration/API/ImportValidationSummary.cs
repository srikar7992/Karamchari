namespace Karamchari.DataMigration.API;

public record ImportValidationSummary(
    Guid JobId,
    int ValidRows,
    int InvalidRows,
    IEnumerable<RowError> Errors);

public record RowError(int RowNumber, string ErrorMessage);
