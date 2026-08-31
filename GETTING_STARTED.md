# Getting Started with Document Agent

## Quick Start (5 minutes)

### 1. Prerequisites

- .NET 8.0 SDK installed
- Azure subscription
- Visual Studio Code or Visual Studio 2022

### 2. Clone and Setup

```bash
git clone https://github.com/mnagakumar/azure-ai-document-agent-mcp.git
cd azure-ai-document-agent-mcp
```

### 3. Configure Azure

#### Using User Secrets (Development)

```bash
cd src/DocumentAgent.Api

dotnet user-secrets init
dotnet user-secrets set "Azure:Storage:ConnectionString" "your-storage-connection-string"
dotnet user-secrets set "Azure:OpenAI:Endpoint" "https://your-resource.openai.azure.com/"
dotnet user-secrets set "Azure:OpenAI:ApiKey" "your-openai-api-key"
```

#### Using appsettings.json (Production)

Edit `src/DocumentAgent.Api/appsettings.json`:
```json
{
  "Azure": {
    "Storage": {
      "ConnectionString": "your-connection-string"
    },
    "OpenAI": {
      "Endpoint": "https://your-resource.openai.azure.com/",
      "ApiKey": "your-api-key",
      "DeploymentName": "gpt-4"
    }
  }
}
```

### 4. Run the Application

```bash
cd src/DocumentAgent.Api
dotnet run
```

The application will start at `http://localhost:5000`

### 5. Test the API

Visit Swagger UI: `http://localhost:5000/swagger/ui`

## First Steps

### Upload a Document

```bash
curl -X POST "http://localhost:5000/api/documents/upload" \
  -F "file=@test.pdf"
```

You'll receive a response with the document ID:
```json
{
  "documentId": "550e8400-e29b-41d4-a716-446655440000",
  "fileName": "test.pdf",
  "status": "Pending"
}
```

### Verify the Document

```bash
curl -X POST "http://localhost:5000/api/verification/550e8400-e29b-41d4-a716-446655440000/verify"
```

### Check Status

```bash
curl "http://localhost:5000/api/documents/550e8400-e29b-41d4-a716-446655440000"
```

## Common Issues

### "Connection failed to Azure Storage"

- Verify connection string is correct
- Check firewall settings
- Ensure storage account exists

### "OpenAI API Error 401"

- Verify API key is correct
- Check endpoint URL format
- Ensure deployment name matches your Azure OpenAI resource

### "Build fails with missing dependencies"

```bash
dotnet restore --force
```

## Next Steps

1. **Integrate MCP**: Connect with Claude or other AI assistants
2. **Add Authentication**: Implement JWT or OAuth
3. **Deploy to Azure**: Use App Service or Container Instances
4. **Monitor**: Set up Application Insights for monitoring
5. **Optimize**: Add caching and batch processing

## Resources

- [Azure SDK Documentation](https://learn.microsoft.com/azure/)
- [Model Context Protocol](https://modelcontextprotocol.io/)
- [ASP.NET Core Documentation](https://learn.microsoft.com/aspnet/core/)
- [MediatR Documentation](https://github.com/jbogard/MediatR)

## Need Help?

Check the [README.md](../README.md) for comprehensive documentation.
