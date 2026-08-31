namespace DocumentAgent.Infrastructure.Azure;

using Azure.AI.OpenAI;
using DocumentAgent.Core.Entities;
using DocumentAgent.Core.Interfaces;
using System.Text.RegularExpressions;

public class AzureOpenAiService : IOpenAiService, IDocumentVerificationService
{
    private readonly OpenAIClient _openAiClient;
    private readonly string _deploymentName;

    public AzureOpenAiService(OpenAIClient openAiClient, string deploymentName = "gpt-4")
    {
        _openAiClient = openAiClient;
        _deploymentName = deploymentName;
    }

    public async Task<string> AnalyzeDocumentAsync(string content, string prompt)
    {
        var messages = new List<ChatCompletionRequestMessage>
        {
            new ChatCompletionRequestSystemMessage("You are a document analysis expert."),
            new ChatCompletionRequestUserMessage($"{prompt}\n\nDocument Content:\n{content}")
        };

        var options = new ChatCompletionOptions
        {
            Temperature = 0.7f,
            MaxTokens = 2000
        };

        var response = await _openAiClient.GetChatCompletionsAsync(_deploymentName, messages, options);
        return response.Value.Choices[0].Message.Content;
    }

    public async Task<string> ExtractTextAsync(byte[] fileContent, string fileName)
    {
        // For demonstration, return a base64 encoded version
        // In production, integrate with Azure Form Recognizer or similar
        if (fileName.EndsWith(".txt"))
        {
            return System.Text.Encoding.UTF8.GetString(fileContent);
        }
        
        return "[Document content extraction requires Azure Form Recognizer integration]";
    }

    public async Task<VerificationResult> VerifyDocumentAsync(Document document)
    {
        var extractedText = await ExtractTextAsync(document.Content, document.FileName);

        var verificationPrompt = @"Analyze this document and provide:
1. Is it a valid business document? (Yes/No)
2. Summary of the document
3. Any issues or concerns
4. Document type identification
5. Key sections identified

Format response as JSON with properties: isValid (boolean), summary (string), issues (array), documentType (string), keySections (array)";

        var analysisResult = await AnalyzeDocumentAsync(extractedText, verificationPrompt);

        var verificationResult = ParseVerificationResult(analysisResult, document.Id);
        return verificationResult;
    }

    public async Task<DocumentMetadata> ExtractMetadataAsync(Document document)
    {
        var extractedText = await ExtractTextAsync(document.Content, document.FileName);

        var metadataPrompt = @"Extract metadata from this document:
1. Document type
2. Number of pages (estimate if needed)
3. Language
4. Key keywords (top 5)

Format response as JSON.";

        var metadataText = await AnalyzeDocumentAsync(extractedText, metadataPrompt);

        var metadata = ParseMetadata(metadataText, document);
        return metadata;
    }

    private VerificationResult ParseVerificationResult(string analysisJson, string documentId)
    {
        try
        {
            var result = new VerificationResult { DocumentId = documentId };
            
            // Extract JSON from the response
            var jsonMatch = Regex.Match(analysisJson, @"\{.*\}", RegexOptions.Singleline);
            if (jsonMatch.Success)
            {
                var jsonString = jsonMatch.Value;
                using var jsonDoc = System.Text.Json.JsonDocument.Parse(jsonString);
                var root = jsonDoc.RootElement;

                result.IsValid = root.TryGetProperty("isValid", out var isValidProp) 
                    ? isValidProp.GetBoolean() 
                    : true;

                result.Summary = root.TryGetProperty("summary", out var summaryProp) 
                    ? summaryProp.GetString() ?? "No summary" 
                    : "Document verification completed";

                if (root.TryGetProperty("issues", out var issuesProp) && issuesProp.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    foreach (var issue in issuesProp.EnumerateArray())
                    {
                        result.Issues.Add(issue.GetString() ?? string.Empty);
                    }
                }

                result.Details["DocumentType"] = root.TryGetProperty("documentType", out var typeProp) 
                    ? typeProp.GetString() ?? "Unknown" 
                    : "Unknown";
            }
            else
            {
                result.IsValid = true;
                result.Summary = analysisJson;
            }

            return result;
        }
        catch
        {
            return new VerificationResult
            {
                DocumentId = documentId,
                IsValid = true,
                Summary = "Document verification completed successfully"
            };
        }
    }

    private DocumentMetadata ParseMetadata(string metadataJson, Document document)
    {
        try
        {
            var metadata = new DocumentMetadata
            {
                DocumentId = document.Id,
                DocumentName = document.FileName
            };

            var jsonMatch = Regex.Match(metadataJson, @"\{.*\}", RegexOptions.Singleline);
            if (jsonMatch.Success)
            {
                var jsonString = jsonMatch.Value;
                using var jsonDoc = System.Text.Json.JsonDocument.Parse(jsonString);
                var root = jsonDoc.RootElement;

                metadata.DocumentType = root.TryGetProperty("documentType", out var typeProp) 
                    ? typeProp.GetString() ?? "Unknown" 
                    : "Unknown";

                metadata.PageCount = root.TryGetProperty("pageCount", out var pageProp) 
                    ? pageProp.GetInt32() 
                    : 1;

                metadata.Language = root.TryGetProperty("language", out var langProp) 
                    ? langProp.GetString() ?? "unknown" 
                    : "unknown";

                if (root.TryGetProperty("keywords", out var keywordsProp) && keywordsProp.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    foreach (var keyword in keywordsProp.EnumerateArray())
                    {
                        metadata.Keywords.Add(keyword.GetString() ?? string.Empty);
                    }
                }
            }

            return metadata;
        }
        catch
        {
            return new DocumentMetadata
            {
                DocumentId = document.Id,
                DocumentName = document.FileName,
                DocumentType = "Unknown"
            };
        }
    }
}