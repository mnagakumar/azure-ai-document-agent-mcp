namespace DocumentAgent.Core.Handlers;

using MediatR;
using DocumentAgent.Core.Commands;
using DocumentAgent.Core.Entities;
using DocumentAgent.Core.Interfaces;
using DocumentAgent.Core.Models;

public class UploadDocumentCommandHandler : IRequestHandler<UploadDocumentCommand, DocumentUploadResponse>
{
    private readonly IDocumentRepository _repository;
    private readonly IBlobStorageService _blobService;

    public UploadDocumentCommandHandler(IDocumentRepository repository, IBlobStorageService blobService)
    {
        _repository = repository;
        _blobService = blobService;
    }

    public async Task<DocumentUploadResponse> Handle(UploadDocumentCommand request, CancellationToken cancellationToken)
    {
        var document = new Document
        {
            FileName = request.FileName,
            ContentType = request.ContentType,
            Content = request.Content,
            FileSize = request.Content.Length,
            Status = DocumentStatus.Pending,
            Metadata = request.Metadata ?? new()
        };

        // Upload to blob storage
        var blobUri = await _blobService.UploadAsync(
            "documents",
            $"{document.Id}/{request.FileName}",
            request.Content,
            request.ContentType);

        document.BlobUri = blobUri;

        // Save to database
        var savedDocument = await _repository.CreateAsync(document);

        return new DocumentUploadResponse
        {
            DocumentId = savedDocument.Id,
            FileName = savedDocument.FileName,
            Status = savedDocument.Status.ToString(),
            UploadedAt = savedDocument.UploadedAt,
            Message = "Document uploaded successfully and queued for verification"
        };
    }
}