namespace DocumentAgent.Core.Entities;

public class Document
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public DocumentStatus Status { get; set; } = DocumentStatus.Pending;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public DateTime? VerifiedAt { get; set; }
    public string? VerificationResult { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
    public string BlobUri { get; set; } = string.Empty;
}

public enum DocumentStatus
{
    Pending = 0,
    Processing = 1,
    Verified = 2,
    Failed = 3,
    Rejected = 4
}