# Document Agent - Azure AI & MCP Integration

A comprehensive .NET 8 application that integrates Azure cloud services with the Model Context Protocol (MCP) for intelligent document processing and verification using Azure OpenAI.

## Features

- 📄 **Document Upload & Management**: Upload and manage documents with Azure Blob Storage
- 🤖 **AI-Powered Verification**: Automatic document verification using Azure OpenAI (GPT-4)
- 📊 **Document Analysis**: Extract metadata, analyze content, and generate summaries
- 🔌 **MCP Integration**: Full Model Context Protocol support for AI assistants
- 💾 **Scalable Storage**: Azure Table Storage for document metadata and status tracking
- 🌐 **RESTful API**: Comprehensive REST API with Swagger/OpenAPI documentation
- 🔐 **Production Ready**: Enterprise-grade error handling and logging

## Architecture

### Project Structure

```
src/
├── DocumentAgent.Core/              # Core entities and interfaces
│   ├── Entities/                    # Domain models
│   ├── Interfaces/                  # Service contracts
│   ├── Commands/                    # MediatR commands
│   ├── Handlers/                    # MediatR handlers
│   └── Models/                      # DTOs and response models
├── DocumentAgent.Infrastructure/    # Azure service implementations
│   └── Azure/
│       ├── AzureBlobStorageService.cs
│       ├── AzureTableStorageRepository.cs
│       └── AzureOpenAiService.cs
├── DocumentAgent.Mcp/               # MCP server implementation
│   ├── Models/
│   └── DocumentAgentMcpServer.cs
└── DocumentAgent.Api/               # ASP.NET Core API
    ├── Controllers/
    ├── appsettings.json
    └── Program.cs
```

### Technology Stack

- **.NET 8.0** - Latest LTS version
- **ASP.NET Core** - Web API framework
- **Azure Storage Blobs** - Document storage
- **Azure Storage Tables** - Metadata storage
- **Azure OpenAI** - AI-powered document analysis
- **MediatR** - Command/query pattern implementation
- **Swagger/OpenAPI** - API documentation

## Getting Started

### Prerequisites

- .NET 8.0 SDK or later
- Azure subscription with:
  - Storage Account (Blob + Table)
  - Azure OpenAI resource with GPT-4 deployment
- Visual Studio 2022 or VS Code with C# extension

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/mnagakumar/azure-ai-document-agent-mcp.git
   cd azure-ai-document-agent-mcp
   ```

2. **Configure Azure credentials**

   Update `src/DocumentAgent.Api/appsettings.json`:
   ```json
   {
     "Azure": {
       "Storage": {
         "ConnectionString": "your-storage-connection-string"
       },
       "OpenAI": {
         "Endpoint": "https://your-resource.openai.azure.com/",
         "ApiKey": "your-openai-api-key",
         "DeploymentName": "gpt-4"
       }
     }
   }
   ```

3. **Restore dependencies and build**
   ```bash
   dotnet restore
   dotnet build
   ```

4. **Run the application**
   ```bash
   cd src/DocumentAgent.Api
   dotnet run
   ```

5. **Access the API**
   - Swagger UI: `http://localhost:5000/swagger/ui`
   - API Base: `http://localhost:5000/api`

## API Documentation

### Document Upload

**POST** `/api/documents/upload`

Upload a document for processing.

**Request:**
```bash
curl -X POST "http://localhost:5000/api/documents/upload" \
  -F "file=@document.pdf" \
  -F "metadata={\"source\": \"email\", \"priority\": \"high\"}"
```

**Response:**
```json
{
  "documentId": "550e8400-e29b-41d4-a716-446655440000",
  "fileName": "document.pdf",
  "status": "Pending",
  "uploadedAt": "2024-08-31T10:30:00Z",
  "blobUri": "https://storage.blob.core.windows.net/documents/file.pdf"
}
```

### Get Document Status

**GET** `/api/documents/{documentId}`

Retrieve the status and details of a document.

**Response:**
```json
{
  "documentId": "550e8400-e29b-41d4-a716-446655440000",
  "fileName": "document.pdf",
  "status": "Verified",
  "uploadedAt": "2024-08-31T10:30:00Z",
  "verifiedAt": "2024-08-31T10:35:00Z",
  "verificationResult": "Valid business document",
  "contentType": "application/pdf",
  "fileSize": 102400,
  "blobUri": "https://storage.blob.core.windows.net/documents/file.pdf",
  "metadata": {
    "source": "email",
    "priority": "high"
  }
}
```

### Verify Document

**POST** `/api/verification/{documentId}/verify`

Perform AI-powered verification on a document.

**Response:**
```json
{
  "documentId": "550e8400-e29b-41d4-a716-446655440000",
  "isValid": true,
  "summary": "This is a valid business contract dated 2024-08-31",
  "issues": [],
  "details": {
    "documentType": "Contract",
    "pageCount": 5,
    "language": "en"
  }
}
```

### List Documents

**GET** `/api/documents`

List all documents in the system.

**Response:**
```json
[
  {
    "documentId": "550e8400-e29b-41d4-a716-446655440000",
    "fileName": "document.pdf",
    "status": "Verified",
    "uploadedAt": "2024-08-31T10:30:00Z"
  }
]
```

### MCP Endpoints

**POST** `/api/mcp/invoke`

Process Model Context Protocol requests.

**GET** `/api/mcp/tools`

List available MCP tools.

**GET** `/api/mcp/resources`

List available MCP resources.

## MCP Integration

The Document Agent implements the Model Context Protocol, enabling integration with AI assistants like Claude. Available tools:

- **upload_document** - Upload a document for processing
- **verify_document** - Verify a document using AI analysis
- **get_document_status** - Retrieve document status and details
- **list_documents** - List all documents in the system

### Example MCP Request

```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "tools/call",
  "params": {
    "name": "upload_document",
    "arguments": {
      "fileName": "contract.pdf",
      "contentType": "application/pdf",
      "content": "base64-encoded-content",
      "metadata": {
        "department": "legal"
      }
    }
  }
}
```

## Configuration

### Environment Variables

Alternatively, use environment variables instead of `appsettings.json`:

```bash
export Azure__Storage__ConnectionString="your-connection-string"
export Azure__OpenAI__Endpoint="https://your-resource.openai.azure.com/"
export Azure__OpenAI__ApiKey="your-api-key"
export Azure__OpenAI__DeploymentName="gpt-4"
```

### Azure Storage Setup

1. Create a Storage Account in Azure
2. Create a Blob Container named `documents`
3. Enable Table Storage
4. Copy the connection string to your configuration

### Azure OpenAI Setup

1. Create an Azure OpenAI resource
2. Deploy a GPT-4 model
3. Copy the endpoint and API key

## Document Status Flow

```
Uploaded → Pending → Verified → Archived
            ↓
          Failed → Error
```

- **Uploaded**: Document received and stored in blob storage
- **Pending**: Awaiting verification
- **Verified**: Successfully analyzed and verified
- **Failed**: Verification failed
- **Archived**: Document archived

## Error Handling

The API returns standardized error responses:

```json
{
  "error": "Document not found"
}
```

HTTP Status Codes:
- `200 OK` - Success
- `400 Bad Request` - Invalid input
- `404 Not Found` - Resource not found
- `500 Internal Server Error` - Server error

## Development

### Running Tests

```bash
dotnet test
```

### Building for Production

```bash
dotnet publish -c Release -o ./publish
```

### Docker Support

Create a `Dockerfile` in the root:

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 5000
ENTRYPOINT ["dotnet", "DocumentAgent.Api.dll"]
```

Build and run:
```bash
docker build -t document-agent .
docker run -p 5000:5000 document-agent
```

## Deployment

### Azure App Service

1. Create an App Service plan
2. Create a Web App
3. Deploy using Visual Studio Publish or GitHub Actions
4. Configure application settings with Azure credentials

### GitHub Actions

The repository includes CI/CD workflows. Configure secrets:
- `AZURE_PUBLISH_PROFILE` - App Service publish profile
- `AZURE_STORAGE_CONNECTION_STRING` - Storage connection string
- `AZURE_OPENAI_ENDPOINT` - OpenAI endpoint
- `AZURE_OPENAI_API_KEY` - OpenAI API key

## Performance Considerations

- **Blob Storage**: Supports documents up to 4.75 TB
- **Table Storage**: Scales to handle millions of document records
- **OpenAI API**: Consider rate limiting and costs
- **Caching**: Implement Redis caching for frequently accessed documents
- **Async Processing**: Use background jobs for large document batches

## Security Best Practices

1. ✅ Use Azure Key Vault for sensitive credentials
2. ✅ Enable Azure Storage encryption
3. ✅ Use managed identities for Azure services
4. ✅ Implement API authentication (add OAuth/JWT if needed)
5. ✅ Enable CORS only for trusted domains
6. ✅ Use HTTPS in production
7. ✅ Regularly update NuGet packages

## Troubleshooting

### Connection String Issues

```bash
# Verify Azure Storage connection
dotnet user-secrets set "Azure:Storage:ConnectionString" "your-connection-string"
```

### OpenAI API Errors

- Verify endpoint URL format
- Check API key validity
- Ensure deployment name matches
- Review Azure OpenAI quota and pricing

### Document Upload Failures

- Check file size and format
- Verify blob storage permissions
- Review application logs

## Monitoring and Logging

Logs are output to console and debug output. Configure additional logging providers:

```csharp
builder.Logging.AddApplicationInsights();
builder.Logging.AddAzureWebAppDiagnostics();
```

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Support

For issues, questions, or suggestions:
- Open an issue on GitHub
- Check existing documentation
- Review API Swagger documentation

## Acknowledgments

- Azure SDK for .NET
- Model Context Protocol (MCP)
- MediatR library
- ASP.NET Core team

## Roadmap

- [ ] Add authentication and authorization
- [ ] Implement batch document processing
- [ ] Add document preview functionality
- [ ] Support for document OCR
- [ ] Advanced filtering and search
- [ ] Document versioning
- [ ] Webhook notifications
- [ ] Mobile app integration

---

**Last Updated**: August 31, 2024
**Version**: 1.0.0
