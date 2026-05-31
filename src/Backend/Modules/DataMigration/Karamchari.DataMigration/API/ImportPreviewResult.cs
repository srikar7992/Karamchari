namespace Karamchari.DataMigration.API;

public record ImportPreviewResult(
    Guid JobId,
    string ImportType,
    int TotalRowsPreviewed,
    IEnumerable<object> Rows);
