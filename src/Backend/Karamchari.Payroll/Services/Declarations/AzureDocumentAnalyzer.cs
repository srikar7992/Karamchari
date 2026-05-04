using Azure;
using Azure.AI.DocumentIntelligence;
using Microsoft.Extensions.Configuration;

namespace Karamchari.Payroll.Services.Declarations;

public sealed class AzureDocumentAnalyzer : IDocumentAnalyzer
{
    private readonly DocumentIntelligenceClient _client;

    public AzureDocumentAnalyzer(IConfiguration configuration)
    {
        var endpoint = configuration["Azure:DocumentIntelligence:Endpoint"];
        var key = configuration["Azure:DocumentIntelligence:Key"];
        
        if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(key))
        {
            // For development, we'll allow it to be null if not configured, 
            // but real calls will fail.
            _client = null!;
            return;
        }

        _client = new DocumentIntelligenceClient(new Uri(endpoint), new AzureKeyCredential(key));
    }

    public async Task<DocumentAnalysisResult> AnalyzeAsync(Stream stream, string fileName)
    {
        if (_client == null) throw new InvalidOperationException("Azure Document Intelligence is not configured.");

        // We use the "prebuilt-receipt" model for common investment proofs 
        // or "prebuilt-layout" for general extraction. "prebuilt-document" is also a good general choice.
        var operation = await _client.AnalyzeDocumentAsync(
            WaitUntil.Completed,
            "prebuilt-document",
            BinaryData.FromStream(stream));

        var result = operation.Value;
        
        // Simple heuristic-based category detection
        string text = string.Join(" ", result.Pages.SelectMany(p => p.Lines.Select(l => l.Content))).ToUpperInvariant();
        string category = InferCategory(text, fileName);

        // Extracting fields from prebuilt-document
        // Note: Real-world extraction would look for specific fields like 'TotalAmount' or 'Date'
        decimal? amount = ExtractAmount(result);
        DateTime? date = ExtractDate(result);
        string? vendor = ExtractVendor(result);

        // 4. Return: Return normalized DTO with Storage URI for audit linkage
        // Real confidence would be an average of field confidences
        float confidence = result.Pages.Count > 0 ? 0.95f : 0.0f;
        
        // Low confidence heuristic (simulated)
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

    private static decimal? ExtractAmount(AnalyzeResult result)
    {
        // In prebuilt-document, we might look for key-value pairs or patterns
        // This is a simplified version.
        return null; // Implementation would involve iterating result.Fields or result.KeyValuePairs
    }

    private static DateTime? ExtractDate(AnalyzeResult result) => null;
    private static string? ExtractVendor(AnalyzeResult result) => null;
}
