namespace DocumentAgent.Core.Handlers;

using MediatR;
using DocumentAgent.Core.Commands;
using DocumentAgent.Core.Entities;
using DocumentAgent.Core.Interfaces;
using DocumentAgent.Core.Models;

public class VerifyDocumentCommandHandler : IRequestHandler<VerifyDocumentCommand, DocumentVerificationResponse>
{
    private readonly IDocumentRepository _repository;
    private readonly IDocumentVerificationService _verificationService;
    private readonly IBlobStorageService _blobService;

    public VerifyDocumentCommandHandler(
        IDocumentRepository repository,
        IDocumentVerificationService verificationService,
        IBlobStorageService blobService)
    {
        _repository = repository;
        _verificationService = verificationService;
        _blobService = blobService;
    }

    public async Task<DocumentVerificationResponse> Handle(VerifyDocumentCommand request, CancellationToken cancellationToken)
    {
        var document = await _repository.GetByIdAsync(request.DocumentId)
            ?? throw new InvalidOperationException($"Document {request.DocumentId} not found");

        try
        {
            document.Status = DocumentStatus.Processing;
            await _repository.UpdateAsync(document);

            var verificationResult = await _verificationService.VerifyDocumentAsync(document);

            document.Status = verificationResult.IsValid ? DocumentStatus.Verified : DocumentStatus.Rejected;
            document.VerifiedAt = verificationResult.VerifiedAt;
            document.VerificationResult = System.Text.Json.JsonSerializer.Serialize(verificationResult);
            await _repository.UpdateAsync(document);

            return new DocumentVerificationResponse
            {
                DocumentId = document.Id,
                IsValid = verificationResult.IsValid,
                Summary = verificationResult.Summary,
                Issues = verificationResult.Issues,
                Details = verificationResult.Details,
                VerifiedAt = verificationResult.VerifiedAt
            };
        }
        catch (Exception ex)
        {
            document.Status = DocumentStatus.Failed;
            document.ErrorMessage = ex.Message;
            await _repository.UpdateAsync(document);
            throw;
        }
    }
}