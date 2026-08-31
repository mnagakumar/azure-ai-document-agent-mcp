# Deploying to Azure Cloud Foundry (Open Service Broker)

This guide explains how to deploy Document Agent to Azure Cloud Foundry and the code changes required.

## Overview

Azure Cloud Foundry is a Platform-as-a-Service (PaaS) that requires specific configuration for .NET applications. The main differences from traditional App Service deployment are:

1. **Environment Variable Configuration** - Services are auto-discovered via `VCAP_SERVICES`
2. **Port Binding** - Must listen on port assigned by Cloud Foundry
3. **Buildpack** - Uses .NET Core buildpack
4. **Service Connections** - Uses service bindings for Azure Storage and OpenAI

## Code Changes Required

### 1. Update Program.cs to Support Cloud Foundry Bindings

**File**: `src/DocumentAgent.Api/Program.cs`

```csharp
using DocumentAgent.Api;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Add Swagger/OpenAPI
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Document Agent API",
        Version = "v1",
        Description = "API for document upload, verification, and MCP integration"
    });
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

// Add Document Agent Services with Cloud Foundry support
builder.Services.AddDocumentAgentServices(builder.Configuration);

// Add logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// **NEW: Configure port from Cloud Foundry environment**
var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
var urls = $"http://0.0.0.0:{port}";

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

// **NEW: Listen on Cloud Foundry port**
await app.RunAsync(urls);
```

### 2. Update ServiceCollectionExtensions for Cloud Foundry

**File**: `src/DocumentAgent.Api/ServiceCollectionExtensions.cs`

```csharp
namespace DocumentAgent.Api;

using DocumentAgent.Core.Interfaces;
using DocumentAgent.Infrastructure.Azure;
using DocumentAgent.Mcp;
using Azure.Storage.Blobs;
using Azure.Storage.Tables;
using Azure.AI.OpenAI;
using MediatR;
using DocumentAgent.Core.Commands;
using DocumentAgent.Core.Handlers;
using System.Text.Json;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDocumentAgentServices(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // **NEW: Try to parse Cloud Foundry VCAP_SERVICES environment variable**
        var vcapServices = Environment.GetEnvironmentVariable("VCAP_SERVICES");
        if (!string.IsNullOrEmpty(vcapServices))
        {\n            ParseCloudFoundryServices(services, vcapServices, configuration);\n        }\n        else\n        {\n            // Fallback to traditional configuration\n            AddAzureServices(services, configuration);\n        }\n\n        // MediatR\n        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(\n            typeof(UploadDocumentCommand).Assembly,\n            typeof(UploadDocumentCommandHandler).Assembly\n        ));\n\n        // MCP Server\n        services.AddScoped<IMcpServer, DocumentAgentMcpServer>();\n\n        return services;\n    }\n\n    // **NEW: Parse Cloud Foundry VCAP_SERVICES**\n    private static void ParseCloudFoundryServices(\n        IServiceCollection services,\n        string vcapServicesJson,\n        IConfiguration configuration)\n    {\n        try\n        {\n            using var jsonDoc = JsonDocument.Parse(vcapServicesJson);\n            var root = jsonDoc.RootElement;\n\n            // Parse Azure Storage\n            if (root.TryGetProperty(\"azure-storage\", out var storageServices))\n            {\n                var storage = storageServices[0];\n                var credentials = storage.GetProperty(\"credentials\");\n                var connectionString = credentials.GetProperty(\"connection_string\").GetString();\n\n                services.AddSingleton(_ => new BlobServiceClient(connectionString));\n                services.AddScoped<IBlobStorageService, AzureBlobStorageService>();\n\n                services.AddSingleton(_ => new TableServiceClient(connectionString));\n                services.AddScoped<IDocumentRepository, AzureTableStorageRepository>();\n            }\n\n            // Parse Azure OpenAI\n            if (root.TryGetProperty(\"azure-openai\", out var openAiServices))\n            {\n                var openAi = openAiServices[0];\n                var credentials = openAi.GetProperty(\"credentials\");\n                var endpoint = credentials.GetProperty(\"endpoint\").GetString();\n                var apiKey = credentials.GetProperty(\"key\").GetString();\n                var deployment = credentials.TryGetProperty(\"deployment_name\", out var depProp)\n                    ? depProp.GetString() ?? \"gpt-4\"\n                    : \"gpt-4\";\n\n                var openAiClient = new OpenAIClient(\n                    new Uri(endpoint),\n                    new Azure.AzureKeyCredential(apiKey));\n\n                services.AddScoped<IOpenAiService>(_ =>\n                    new AzureOpenAiService(openAiClient, deployment));\n                services.AddScoped<IDocumentVerificationService>(_ =>\n                    new AzureOpenAiService(openAiClient, deployment));\n            }\n        }\n        catch (Exception ex)\n        {\n            Console.WriteLine($\"Error parsing VCAP_SERVICES: {ex.Message}. Falling back to configuration.\");\n            AddAzureServices(services, configuration);\n        }\n    }\n\n    // Original Azure service configuration\n    private static void AddAzureServices(\n        IServiceCollection services,\n        IConfiguration configuration)\n    {\n        var blobConnectionString = configuration[\"Azure:Storage:ConnectionString\"];\n        var tableConnectionString = configuration[\"Azure:Storage:ConnectionString\"];\n        var openAiEndpoint = configuration[\"Azure:OpenAI:Endpoint\"];\n        var openAiApiKey = configuration[\"Azure:OpenAI:ApiKey\"];\n        var openAiDeployment = configuration[\"Azure:OpenAI:DeploymentName\"] ?? \"gpt-4\";\n\n        services.AddSingleton(_ => new BlobServiceClient(blobConnectionString));\n        services.AddScoped<IBlobStorageService, AzureBlobStorageService>();\n\n        services.AddSingleton(_ => new TableServiceClient(tableConnectionString));\n        services.AddScoped<IDocumentRepository, AzureTableStorageRepository>();\n\n        services.AddSingleton(_ => new OpenAIClient(\n            new Uri(openAiEndpoint),\n            new Azure.AzureKeyCredential(openAiApiKey)));\n\n        services.AddScoped<IOpenAiService>(provider =>\n        {\n            var client = provider.GetRequiredService<OpenAIClient>();\n            return new AzureOpenAiService(client, openAiDeployment);\n        });\n\n        services.AddScoped<IDocumentVerificationService>(provider =>\n        {\n            return provider.GetRequiredService<AzureOpenAiService>();\n        });\n    }\n}
```

### 3. Create manifest.yml for Cloud Foundry

**File**: `manifest.yml` (in repository root)

```yaml
applications:
- name: document-agent
  buildpack: dotnet_core_buildpack
  runtime: dotnet
  stack: cflinuxfs4
  
  # Memory and disk allocation
  memory: 512M
  disk_quota: 1G
  
  # Instances
  instances: 2
  
  # Environment variables for non-sensitive config
  env:
    ASPNETCORE_ENVIRONMENT: production
    ASPNETCORE_URLS: http://0.0.0.0:8080
    DOTNET_USE_POLLING_FILE_WATCHER: false
  
  # Service bindings
  services:
    - document-agent-storage    # Azure Storage binding
    - document-agent-openai     # Azure OpenAI binding
  
  # Health check
  health-check-type: http
  health-check-http-endpoint: /api/documents
  health-check-invocation-timeout: 5
  
  # Timeout settings
  timeout: 60
```

### 4. Create Runtime Config (.NET BuildPack)

**File**: `.cfignore`

```
.git
.gitignore
.vs
.vscode
bin
obj
test
tests
*.log
*.user
*.suo
.DS_Store
node_modules
publish
```

### 5. Add Cloud Foundry Logging Helper

**File**: `src/DocumentAgent.Api/CloudFoundryLogging.cs`

```csharp
namespace DocumentAgent.Api;

using Microsoft.Extensions.Logging;

/// <summary>
/// Cloud Foundry logging helper - formats logs for proper streaming
/// </summary>
public static class CloudFoundryLogging
{
    public static ILoggingBuilder AddCloudFoundryLogging(this ILoggingBuilder builder)
    {
        // Cloud Foundry expects structured logs to stdout/stderr
        builder.ClearProviders();
        builder.AddConsole();
        builder.AddDebug();
        
        return builder;
    }
}
```

## Deployment Steps

### 1. Install Cloud Foundry CLI

```bash
# macOS
brew install cloudfoundry-cli

# Windows or Linux - download from https://github.com/cloudfoundry/cli/releases
```

### 2. Login to Cloud Foundry

```bash
cf login -a https://your-cf-instance.azureapps.io \
  --sso-passcode YOUR_PASSCODE
```

### 3. Create Service Instances

```bash
# Create Azure Storage service
cf create-service azure-storage standard document-agent-storage \
  -c '{
    "storage_account_name": "your-account",
    "storage_account_key": "your-key"
  }'

# Create Azure OpenAI service
cf create-service azure-openai standard document-agent-openai \
  -c '{
    "endpoint": "https://your-resource.openai.azure.com/",
    "key": "your-api-key",
    "deployment_name": "gpt-4"
  }'
```

### 4. Build the Application

```bash
cd src/DocumentAgent.Api
dotnet publish -c Release -o ./publish
```

### 5. Deploy to Cloud Foundry

```bash
cf push
```

### 6. Monitor Deployment

```bash
# Check app status
cf apps

# View recent logs
cf logs document-agent --recent

# Stream logs
cf logs document-agent

# Check app details
cf app document-agent
```

## Troubleshooting

### View Staging Logs

```bash
cf logs document-agent --recent
```

### Check Environment Variables

```bash
cf env document-agent
```

### SSH into Container (if enabled)

```bash
cf ssh document-agent
```

### Common Issues

#### 1. BuildPack Not Found
```bash
# List available buildpacks
cf buildpacks

# If dotnet_core_buildpack missing, install it:
cf create-buildpack dotnet_core_buildpack \
  https://github.com/cloudfoundry/dotnet-core-buildpack/releases/download/...
```

#### 2. Port Binding Error
Ensure `Program.cs` uses `PORT` environment variable (already included in code changes above)

#### 3. Service Connection Failed
Check service bindings:
```bash
cf services
cf service document-agent-storage
```

#### 4. Memory Issues
Increase memory in manifest.yml:
```yaml
memory: 1024M  # Increase from 512M
```

## Performance Tuning

### Scaling

```bash
# Scale instances
cf scale document-agent -i 3

# Auto-scaling (if supported)
cf target -o your-org -s your-space
cf create-autoscaling-rules document-agent
```

### Memory Optimization

Update manifest.yml:
```yaml
memory: 768M  # Optimal for .NET 8 with OpenAI
disk_quota: 2G
```

## Monitoring

### Application Insights Integration

Add to `Program.cs`:
```csharp
// Add Application Insights
builder.Services.AddApplicationInsightsTelemetry(
    configuration["APPINSIGHTS_INSTRUMENTATIONKEY"]);
```

### Health Endpoint

The configured health check uses `/api/documents` endpoint. Ensure it's responsive and lightweight.

## Environment-Specific Configuration

Cloud Foundry sets `ASPNETCORE_ENVIRONMENT` to `production` by default.

Create `appsettings.Production.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Kestrel": {
    "Limits": {
      "MaxRequestBodySize": 52428800
    }
  }
}
```

## Security Considerations

1. **Credentials**: Never commit credentials in manifest.yml - use service bindings
2. **HTTPS**: Cloud Foundry handles SSL termination - disable HTTPS redirect in production
3. **CORS**: Update CORS policy for your specific domains
4. **Secrets**: Use Cloud Foundry's credential service or Azure Key Vault

## Cost Optimization

Cloud Foundry typically charges per:
- App memory usage
- Service instances
- Data transfer

To optimize:
- Use appropriate memory allocation (512M-1G for .NET 8)
- Set reasonable instance count (2-3 for HA)
- Use Azure services efficiently
- Monitor OpenAI API usage

## References

- [Cloud Foundry Documentation](https://docs.cloudfoundry.org/)
- [.NET Core Buildpack](https://github.com/cloudfoundry/dotnet-core-buildpack)
- [Azure Service Broker](https://github.com/Azure/open-service-broker-azure)
- [VCAP_SERVICES Format](https://docs.cloudfoundry.org/devguide/deploy-apps/environment-variable.html)

## Summary of Code Changes

| File | Change | Reason |\n|------|--------|--------|\n| Program.cs | Read PORT from environment | Cloud Foundry assigns dynamic port |\n| ServiceCollectionExtensions.cs | Parse VCAP_SERVICES | Auto-discover Azure services |\n| manifest.yml | New file | Cloud Foundry deployment config |\n| appsettings.Production.json | New file | Production-specific settings |\n| .cfignore | New file | Exclude unnecessary files from upload |\n\nThese changes enable seamless deployment to Azure Cloud Foundry while maintaining backward compatibility with traditional App Service deployment.\n"