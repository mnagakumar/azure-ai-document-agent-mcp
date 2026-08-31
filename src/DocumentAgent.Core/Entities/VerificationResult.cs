namespace DocumentAgent.Core.Entities;

public class VerificationResult
{
    public string DocumentId { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public string Summary { get; set; } = string.Empty;
    public List<string> Issues { get; set; } = new();
    public Dictionary<string, object> Details { get; set; } = new();
    public DateTime VerifiedAt { get; set; } = DateTime.UtcNow;
    public string VerificationEngine { get; set; } = "Azure OpenAI";
}

public class DocumentMetadata
{
    public string DocumentId { get; set; } = string.Empty;
    public string DocumentName { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public int PageCount { get; set; }
    public string Language { get; set; } = "unknown";
    public List<string> Keywords { get; set; } = new();
    public DateTime ExtractedAt { get; set; } = DateTime.UtcNow;
}