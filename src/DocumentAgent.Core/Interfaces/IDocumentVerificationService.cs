namespace DocumentAgent.Core.Interfaces;

using DocumentAgent.Core.Entities;

public interface IDocumentVerificationService
{
    Task<VerificationResult> VerifyDocumentAsync(Document document);
    Task<DocumentMetadata> ExtractMetadataAsync(Document document);
}

public interface IOpenAiService
{
    Task<string> AnalyzeDocumentAsync(string content, string prompt);
    Task<string> ExtractTextAsync(byte[] fileContent, string fileName);
}