namespace Karamchari.Payroll.Services.Declarations;

/// <summary>
/// Configuration options for Azure Document Intelligence.
/// Bound from the <c>"DocumentIntelligence"</c> configuration section.
/// In production, the <see cref="Endpoint"/> and <see cref="Key"/> values are
/// injected from Azure Key Vault via Managed Identity — they are NEVER stored
/// in code, appsettings files, or the repository.
/// </summary>
public sealed class DocumentIntelligenceOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "DocumentIntelligence";

    /// <summary>The Azure Document Intelligence endpoint URL.</summary>
    public string Endpoint { get; init; } = string.Empty;

    /// <summary>
    /// The API key for Azure Document Intelligence.
    /// In production, sourced from Azure Key Vault via Managed Identity.
    /// Leave empty in development to receive a clear <see cref="InvalidOperationException"/>
    /// instead of a null-ref when the SDK is first called.
    /// Matches the <c>"ApiKey"</c> JSON property in <c>appsettings.*.json</c>.
    /// </summary>
    public string ApiKey { get; init; } = string.Empty;
}
