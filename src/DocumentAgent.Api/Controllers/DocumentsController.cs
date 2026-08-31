namespace DocumentAgent.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using MediatR;
using DocumentAgent.Core.Commands;
using DocumentAgent.Core.Models;

[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(IMediator mediator, ILogger<DocumentsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Upload a document for processing
    /// </summary>
    [HttpPost("upload")]
    [ProducesResponseType(typeof(DocumentUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadDocument(IFormFile file, [FromForm] string? metadata)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file provided");

        try
        {
            _logger.LogInformation($"Uploading document: {file.FileName}");

            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream);
                var content = memoryStream.ToArray();

                Dictionary<string, string>? parsedMetadata = null;
                if (!string.IsNullOrEmpty(metadata))
                {
                    try
                    {
                        parsedMetadata = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(metadata);
                    }
                    catch
                    {
                        _logger.LogWarning("Failed to parse metadata JSON");
                    }
                }

                var command = new UploadDocumentCommand
                {
                    FileName = file.FileName,
                    ContentType = file.ContentType,
                    Content = content,
                    Metadata = parsedMetadata
                };

                var result = await _mediator.Send(command);
                return Ok(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading document");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get the status of a document
    /// </summary>
    [HttpGet("{documentId}")]
    [ProducesResponseType(typeof(DocumentStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDocumentStatus(string documentId)
    {
        try
        {
            _logger.LogInformation($"Getting status for document: {documentId}");

            var command = new GetDocumentStatusCommand { DocumentId = documentId };
            var result = await _mediator.Send(command);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, $"Document not found: {documentId}");
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting document status");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// List all documents
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<DocumentStatusResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListDocuments()
    {
        try
        {
            _logger.LogInformation("Listing all documents");

            var command = new ListDocumentsCommand();
            var result = await _mediator.Send(command);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing documents");
            return BadRequest(new { error = ex.Message });
        }
    }
}
