using Azure;
using Azure.AI.DocumentIntelligence;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Karamchari.HR.Services;

internal sealed class DocumentIntelligenceService : IDocumentIntelligenceService
{
    private readonly DocumentIntelligenceClient _client;
    private readonly ILogger<DocumentIntelligenceService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentIntelligenceService"/> class.
    /// </summary>
    /// <param name="options">The document intelligence options.</param>
    /// <param name="logger">The logger.</param>
    public DocumentIntelligenceService(
        IOptions<DocumentIntelligenceOptions> options,
        ILogger<DocumentIntelligenceService> logger)
    {
        _logger = logger;
        
        var endpoint = options.Value.Endpoint;
        var apiKey = options.Value.ApiKey;

        if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("Document Intelligence Endpoint or ApiKey is missing. AI features will fail.");
        }

        var credential = new AzureKeyCredential(apiKey ?? string.Empty);
        _client = new DocumentIntelligenceClient(new Uri(endpoint ?? "https://localhost"), credential);
    }

    /// <summary>
    /// Analyzes an ID document stream and extracts key information.
    /// </summary>
    /// <param name="documentStream">The document stream.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The analyzed document result.</returns>
    public async Task<AnalyzedDocumentResult> AnalyzeIdDocumentAsync(Stream documentStream, CancellationToken cancellationToken = default)
    {
        try
        {
            var content = new AnalyzeDocumentContent()
            {
                Base64Source = BinaryData.FromStream(documentStream)
            };

            Operation<AnalyzeResult> operation = await _client.AnalyzeDocumentAsync(
                WaitUntil.Completed,
                "prebuilt-idDocument",
                content,
                cancellationToken: cancellationToken);

            AnalyzeResult result = operation.Value;

            if (result.Documents.Count == 0)
            {
                throw new InvalidOperationException("No document data extracted.");
            }

            var doc = result.Documents[0];
            
            var firstName = ExtractStringField(doc, "FirstName");
            var lastName = ExtractStringField(doc, "LastName");
            var idNumber = ExtractStringField(doc, "DocumentNumber");
            
            var dobField = doc.Fields.TryGetValue("DateOfBirth", out var f) ? f : null;
            DateOnly? dateOfBirth = null;
            if (dobField != null && dobField.ValueDate.HasValue)
            {
                dateOfBirth = DateOnly.FromDateTime(dobField.ValueDate.Value.DateTime);
            }

            var legalName = string.IsNullOrWhiteSpace(lastName) 
                ? firstName 
                : $"{firstName} {lastName}".Trim();

            return new AnalyzedDocumentResult(legalName ?? string.Empty, dateOfBirth, idNumber ?? string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while analyzing document.");
            throw;
        }
    }

    private static string? ExtractStringField(AnalyzedDocument doc, string fieldName)
    {
        if (doc.Fields.TryGetValue(fieldName, out var field))
        {
            return field.ValueString;
        }
        return null;
    }
}
