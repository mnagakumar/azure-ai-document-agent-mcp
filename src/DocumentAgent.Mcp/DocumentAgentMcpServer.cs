namespace DocumentAgent.Mcp;

using System.Text.Json;
using System.Text.Json.Serialization;
using DocumentAgent.Core.Commands;
using MediatR;

public interface IMcpServer
{
    Task<McpResponse> ProcessRequestAsync(McpRequest request);
    List<McpTool> GetAvailableTools();
    List<McpResource> GetAvailableResources();
}

public class DocumentAgentMcpServer : IMcpServer
{
    private readonly IMediator _mediator;
    private readonly ILogger<DocumentAgentMcpServer> _logger;

    public DocumentAgentMcpServer(IMediator mediator, ILogger<DocumentAgentMcpServer> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<McpResponse> ProcessRequestAsync(McpRequest request)
    {
        try
        {
            _logger.LogInformation($"Processing MCP request: {request.Method}");

            var result = request.Method switch
            {
                "initialize" => HandleInitialize(request),
                "tools/list" => HandleListTools(request),
                "resources/list" => HandleListResources(request),
                "tools/call" => await HandleToolCall(request),
                "resources/read" => await HandleReadResource(request),
                _ => throw new InvalidOperationException($"Unknown method: {request.Method}")
            };

            return new McpResponse
            {
                JsonRpc = "2.0",
                Id = request.Id,
                Result = result
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error processing MCP request: {request.Method}");
            return new McpResponse
            {
                JsonRpc = "2.0",
                Id = request.Id,
                Error = new McpError
                {
                    Code = -1,
                    Message = ex.Message,
                    Data = ex.StackTrace
                }
            };
        }
    }

    private object HandleInitialize(McpRequest request)
    {
        return new
        {
            protocolVersion = "2024-11-05",
            capabilities = new
            {
                tools = new object(),
                resources = new object()
            },
            serverInfo = new
            {
                name = "Document Agent MCP Server",
                version = "1.0.0"
            }
        };
    }

    private object HandleListTools(McpRequest request)
    {
        return new { tools = GetAvailableTools() };
    }

    private object HandleListResources(McpRequest request)
    {
        return new { resources = GetAvailableResources() };
    }

    private async Task<object> HandleToolCall(McpRequest request)
    {
        if (request.Params == null)
            throw new InvalidOperationException("Tool call requires params");

        var toolName = request.Params.Value.GetProperty("name").GetString();
        var toolArgs = request.Params.Value.GetProperty("arguments");

        _logger.LogInformation($"Calling tool: {toolName}");

        var result = toolName switch
        {
            "upload_document" => await HandleUploadDocument(toolArgs),
            "verify_document" => await HandleVerifyDocument(toolArgs),
            "get_document_status" => await HandleGetDocumentStatus(toolArgs),
            "list_documents" => await HandleListDocuments(toolArgs),
            _ => throw new InvalidOperationException($"Unknown tool: {toolName}")
        };

        return new
        {
            content = new[] { new { type = "text", text = JsonSerializer.Serialize(result) } }
        };
    }

    private async Task<object> HandleReadResource(McpRequest request)
    {
        if (request.Params == null)
            throw new InvalidOperationException("Resource read requires params");

        var uri = request.Params.Value.GetProperty("uri").GetString();
        _logger.LogInformation($"Reading resource: {uri}");

        // Parse resource URI format: documents/[id]
        if (uri?.StartsWith("documents/") == true)
        {
            var documentId = uri.Substring("documents/".Length);
            var command = new GetDocumentStatusCommand { DocumentId = documentId };
            var result = await _mediator.Send(command);

            return new
            {
                contents = new[] { new { uri = uri, mimeType = "application/json", text = JsonSerializer.Serialize(result) } }
            };
        }

        throw new InvalidOperationException($"Unknown resource: {uri}");
    }

    private async Task<object> HandleUploadDocument(JsonElement args)
    {
        var fileName = args.GetProperty("fileName").GetString() ?? throw new InvalidOperationException("fileName required");
        var contentType = args.GetProperty("contentType").GetString() ?? "application/octet-stream";
        var contentBase64 = args.GetProperty("content").GetString() ?? throw new InvalidOperationException("content required");

        var content = Convert.FromBase64String(contentBase64);

        Dictionary<string, string>? metadata = null;
        if (args.TryGetProperty("metadata", out var metadataProp) && metadataProp.ValueKind == System.Text.Json.JsonValueKind.Object)
        {
            metadata = JsonSerializer.Deserialize<Dictionary<string, string>>(metadataProp.GetRawText());
        }

        var command = new UploadDocumentCommand
        {
            FileName = fileName,
            ContentType = contentType,
            Content = content,
            Metadata = metadata
        };

        return await _mediator.Send(command);
    }

    private async Task<object> HandleVerifyDocument(JsonElement args)
    {
        var documentId = args.GetProperty("documentId").GetString() ?? throw new InvalidOperationException("documentId required");

        var command = new VerifyDocumentCommand { DocumentId = documentId };
        return await _mediator.Send(command);
    }

    private async Task<object> HandleGetDocumentStatus(JsonElement args)
    {
        var documentId = args.GetProperty("documentId").GetString() ?? throw new InvalidOperationException("documentId required");

        var command = new GetDocumentStatusCommand { DocumentId = documentId };
        return await _mediator.Send(command);
    }

    private async Task<object> HandleListDocuments(JsonElement args)
    {
        var command = new ListDocumentsCommand();
        return await _mediator.Send(command);
    }

    public List<McpTool> GetAvailableTools()
    {
        return new List<McpTool>
        {
            new McpTool
            {
                Name = "upload_document",
                Description = "Upload a document for processing and verification",
                InputSchema = JsonDocument.Parse(JsonSerializer.Serialize(new
                {
                    type = "object",
                    properties = new
                    {
                        fileName = new { type = "string", description = "Name of the file" },
                        contentType = new { type = "string", description = "MIME type of the file" },
                        content = new { type = "string", description = "Base64 encoded file content" },
                        metadata = new { type = "object", description = "Optional metadata dictionary" }
                    },
                    required = new[] { "fileName", "content" }
                })).RootElement
            },
            new McpTool
            {
                Name = "verify_document",
                Description = "Verify a previously uploaded document using AI analysis",
                InputSchema = JsonDocument.Parse(JsonSerializer.Serialize(new
                {
                    type = "object",
                    properties = new
                    {
                        documentId = new { type = "string", description = "ID of the document to verify" }
                    },
                    required = new[] { "documentId" }
                })).RootElement
            },
            new McpTool
            {
                Name = "get_document_status",
                Description = "Get the status and details of a document",
                InputSchema = JsonDocument.Parse(JsonSerializer.Serialize(new
                {
                    type = "object",
                    properties = new
                    {
                        documentId = new { type = "string", description = "ID of the document" }
                    },
                    required = new[] { "documentId" }
                })).RootElement
            },
            new McpTool
            {
                Name = "list_documents",
                Description = "List all documents in the system",
                InputSchema = JsonDocument.Parse(JsonSerializer.Serialize(new
                {
                    type = "object",
                    properties = new { }
                })).RootElement
            }
        };
    }

    public List<McpResource> GetAvailableResources()
    {
        return new List<McpResource>
        {
            new McpResource
            {
                Uri = "documents://list",
                Name = "Document List",
                Description = "Access to all documents in the system",
                MimeType = "application/json"
            },
            new McpResource
            {
                Uri = "documents://status",
                Name = "Document Status",
                Description = "Access document verification status and results",
                MimeType = "application/json"
            }
        };
    }
}