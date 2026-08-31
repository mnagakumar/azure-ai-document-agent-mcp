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

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDocumentAgentServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Azure Services
        var blobConnectionString = configuration["Azure:Storage:ConnectionString"];
        var tableConnectionString = configuration["Azure:Storage:ConnectionString"];
        var openAiEndpoint = configuration["Azure:OpenAI:Endpoint"];
        var openAiApiKey = configuration["Azure:OpenAI:ApiKey"];
        var openAiDeployment = configuration["Azure:OpenAI:DeploymentName"] ?? "gpt-4";

        // Blob Storage
        services.AddSingleton(_ => new BlobServiceClient(blobConnectionString));
        services.AddScoped<IBlobStorageService, AzureBlobStorageService>();

        // Table Storage
        services.AddSingleton(_ => new TableServiceClient(tableConnectionString));
        services.AddScoped<IDocumentRepository, AzureTableStorageRepository>();

        // OpenAI
        services.AddSingleton(_ => new OpenAIClient(new Uri(openAiEndpoint), new Azure.AzureKeyCredential(openAiApiKey)));
        services.AddScoped<IOpenAiService>(provider =>
        {
            var client = provider.GetRequiredService<OpenAIClient>();
            return new AzureOpenAiService(client, openAiDeployment);
        });
        services.AddScoped<IDocumentVerificationService>(provider =>
        {
            return provider.GetRequiredService<AzureOpenAiService>();
        });

        // MediatR
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(
            typeof(UploadDocumentCommand).Assembly,
            typeof(UploadDocumentCommandHandler).Assembly
        ));

        // MCP Server
        services.AddScoped<IMcpServer, DocumentAgentMcpServer>();

        return services;
    }
}
