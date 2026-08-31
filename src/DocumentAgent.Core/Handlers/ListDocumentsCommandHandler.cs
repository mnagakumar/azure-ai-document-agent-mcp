namespace DocumentAgent.Core.Handlers;

using MediatR;
using DocumentAgent.Core.Commands;
using DocumentAgent.Core.Interfaces;
using DocumentAgent.Core.Models;

public class ListDocumentsCommandHandler : IRequestHandler<ListDocumentsCommand, List<DocumentStatusResponse>>
{
    private readonly IDocumentRepository _repository;

    public ListDocumentsCommandHandler(IDocumentRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<DocumentStatusResponse>> Handle(ListDocumentsCommand request, CancellationToken cancellationToken)
    {
        var documents = await _repository.GetAllAsync();

        return documents.Select(doc => new DocumentStatusResponse
        {
            DocumentId = doc.Id,
            FileName = doc.FileName,
            Status = doc.Status.ToString(),
            UploadedAt = doc.UploadedAt,
            VerifiedAt = doc.VerifiedAt,
            VerificationResult = doc.VerificationResult,
            ErrorMessage = doc.ErrorMessage,
            Metadata = doc.Metadata
        }).ToList();
    }
}