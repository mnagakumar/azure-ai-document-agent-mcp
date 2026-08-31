namespace DocumentAgent.Core.Models;

public class DocumentVerificationRequest
{
    public string DocumentId { get; set; } = string.Empty;
}

public class DocumentVerificationResponse
{
    public string DocumentId { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public string Summary { get; set; } = string.Empty;
    public List<string> Issues { get; set; } = new();
    public Dictionary<string, object> Details { get; set; } = new();
    public DateTime VerifiedAt { get; set; }
}