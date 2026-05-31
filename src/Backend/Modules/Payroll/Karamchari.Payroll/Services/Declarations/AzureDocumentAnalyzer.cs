using Azure;
using Azure.AI.DocumentIntelligence;
using Microsoft.Extensions.Options;

namespace Karamchari.Payroll.Services.Declarations;

/// <summary>
/// Azure Document Intelligence implementation of <see cref="IDocumentAnalyzer"/>.
/// Configuration is resolved from <see cref="DocumentIntelligenceOptions"/> (section
/// <c>"DocumentIntelligence"</c>) so secrets flow from Azure Key Vault via Managed Identity
/// â€” never from raw <c>IConfiguration</c> strings baked into code.
/// </summary>
public sealed class AzureDocumentAnalyzer : IDocumentAnalyzer
{
    private readonly DocumentIntelligenceClient _client;

    /// <summary>
    /// Initializes a new instance of <see cref="AzureDocumentAnalyzer"/>.
    /// </summary>
    /// <param name="options">
    /// Strongly-typed options bound from the <c>"DocumentIntelligence"</c> config section.
    /// In development, leave <c>Endpoint</c> and <c>Key</c> empty â€” the constructor
    /// sets <c>_client = null!</c> and any real call will surface a clear
    /// <see cref="InvalidOperationException"/> rather than a null-ref.
    /// </param>
    public AzureDocumentAnalyzer(IOptions<DocumentIntelligenceOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var cfg = options.Value;

        if (string.IsNullOrEmpty(cfg.Endpoint) || string.IsNullOrEmpty(cfg.ApiKey))
        {
            // Development fallback: allow the service to be registered without real credentials.
            // Any attempt to call AnalyzeAsync will throw a clear InvalidOperationException.
            _client = null!;
            return;
        }

        _client = new DocumentIntelligenceClient(new Uri(cfg.Endpoint), new AzureKeyCredential(cfg.ApiKey));
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public async Task<DocumentAnalysisResult> AnalyzeAsync(Stream stream, string fileName)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(fileName);
        if (_client == null) throw new InvalidOperationException("Azure Document Intelligence is not configured.");

        // Use the typed AnalyzeDocumentContent overload so the SDK returns Operation<AnalyzeResult>
        // directly â€” avoids the raw-protocol BinaryData path and ModelReaderWriter deserialization.
        var content = new AnalyzeDocumentContent { Base64Source = BinaryData.FromStream(stream) };
        var operation = await _client.AnalyzeDocumentAsync(
            WaitUntil.Completed,
            "prebuilt-document",
            content);

        var result = operation.Value;

        string text = string.Join(" ", result.Pages.SelectMany(p => p.Lines.Select(l => l.Content))).ToUpperInvariant();
        string category = InferCategory(text, fileName);

        decimal? amount = ExtractAmount(result);
        DateTime? date = ExtractDate(result);
        string? vendor = ExtractVendor(result);

        float confidence = result.Pages.Count > 0 ? 0.95f : 0.0f;
        if (amount == null || date == null) confidence = 0.5f;

        return new DocumentAnalysisResult
        {
            Category = category,
            ExtractedAmount = amount,
            DocumentDate = date,
            Vendor = vendor,
            Confidence = confidence
        };
    }

    private static string InferCategory(string text, string fileName)
    {
        var name = fileName.ToUpperInvariant();
        if (text.Contains("LIC") || text.Contains("LIFE INSURANCE") || name.Contains("LIC")) return "80C";
        if (text.Contains("RENT") || text.Contains("HRA") || name.Contains("RENT")) return "HRA";
        if (text.Contains("HEALTH") || text.Contains("MEDICAL") || text.Contains("80D")) return "80D";
        if (text.Contains("PPF") || text.Contains("PROVIDENT FUND")) return "80C";
        return "Unknown";
    }

    private static decimal? ExtractAmount(AnalyzeResult result) => null;
    private static DateTime? ExtractDate(AnalyzeResult result) => null;
    private static string? ExtractVendor(AnalyzeResult result) => null;
}
