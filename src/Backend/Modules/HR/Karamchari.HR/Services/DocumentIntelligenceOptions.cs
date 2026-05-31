namespace Karamchari.HR.Services;

/// <summary>
/// Options for Azure Document Intelligence integration used by HR document processing.
/// </summary>
public class DocumentIntelligenceOptions
{
    /// <summary>
    /// Configuration section name for binding document intelligence options.
    /// </summary>
    public const string SectionName = "DocumentIntelligence";

    /// <summary>
    /// Azure Document Intelligence endpoint URI.
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;
    /// <summary>
    /// API key used by local or non-managed-identity document analysis integrations.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
}
