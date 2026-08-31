namespace DocumentAgent.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using MediatR;
using DocumentAgent.Core.Commands;
using DocumentAgent.Core.Models;

[ApiController]
[Route("api/[controller]")]
public class VerificationController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<VerificationController> _logger;

    public VerificationController(IMediator mediator, ILogger<VerificationController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Verify a document using AI analysis
    /// </summary>
    [HttpPost("{documentId}/verify")]
    [ProducesResponseType(typeof(DocumentVerificationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> VerifyDocument(string documentId)
    {
        try
        {
            _logger.LogInformation($"Verifying document: {documentId}");

            var command = new VerifyDocumentCommand { DocumentId = documentId };
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
            _logger.LogError(ex, "Error verifying document");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }
}
