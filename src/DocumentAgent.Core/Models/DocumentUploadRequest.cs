namespace DocumentAgent.Core.Models;

public class DocumentUploadRequest
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public Dictionary<string, string>? Metadata { get; set; }
}

public class DocumentUploadResponse
{
    public string DocumentId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    public string Message { get; set; } = string.Empty;
}