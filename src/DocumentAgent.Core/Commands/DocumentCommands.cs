namespace DocumentAgent.Core.Commands;

using MediatR;
using DocumentAgent.Core.Models;

public class UploadDocumentCommand : IRequest<DocumentUploadResponse>
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public Dictionary<string, string>? Metadata { get; set; }
}

public class VerifyDocumentCommand : IRequest<DocumentVerificationResponse>
{
    public string DocumentId { get; set; } = string.Empty;
}

public class GetDocumentStatusCommand : IRequest<DocumentStatusResponse>
{
    public string DocumentId { get; set; } = string.Empty;
}

public class ListDocumentsCommand : IRequest<List<DocumentStatusResponse>>
{
}