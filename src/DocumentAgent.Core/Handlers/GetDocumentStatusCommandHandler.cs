namespace DocumentAgent.Core.Handlers;

using MediatR;
using DocumentAgent.Core.Commands;
using DocumentAgent.Core.Interfaces;
using DocumentAgent.Core.Models;

public class GetDocumentStatusCommandHandler : IRequestHandler<GetDocumentStatusCommand, DocumentStatusResponse>
{
    private readonly IDocumentRepository _repository;

    public GetDocumentStatusCommandHandler(IDocumentRepository repository)
    {
        _repository = repository;
    }

    public async Task<DocumentStatusResponse> Handle(GetDocumentStatusCommand request, CancellationToken cancellationToken)
    {
        var document = await _repository.GetByIdAsync(request.DocumentId)
            ?? throw new InvalidOperationException($"Document {request.DocumentId} not found");

        return new DocumentStatusResponse
        {
            DocumentId = document.Id,
            FileName = document.FileName,
            Status = document.Status.ToString(),
            UploadedAt = document.UploadedAt,
            VerifiedAt = document.VerifiedAt,
            VerificationResult = document.VerificationResult,
            ErrorMessage = document.ErrorMessage,
            Metadata = document.Metadata
        };
    }
}