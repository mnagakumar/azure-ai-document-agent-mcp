namespace DocumentAgent.Infrastructure.Azure;

using Azure;
using Azure.Data.Tables;
using DocumentAgent.Core.Entities;
using DocumentAgent.Core.Interfaces;

public class AzureTableStorageRepository : IDocumentRepository
{
    private readonly TableClient _tableClient;
    private const string TableName = "Documents";

    public AzureTableStorageRepository(TableServiceClient tableServiceClient)
    {
        _tableClient = tableServiceClient.GetTableClient(TableName);
    }

    public async Task<Document> CreateAsync(Document document)
    {
        var entity = MapDocumentToTableEntity(document);
        await _tableClient.AddEntityAsync(entity);
        return document;
    }

    public async Task<Document?> GetByIdAsync(string id)
    {
        try
        {
            var result = await _tableClient.GetEntityAsync<DocumentTableEntity>(id, id);
            return MapTableEntityToDocument(result.Value);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task<IEnumerable<Document>> GetAllAsync()
    {
        var entities = _tableClient.QueryAsync<DocumentTableEntity>();
        var documents = new List<Document>();

        await foreach (var entity in entities)
        {
            documents.Add(MapTableEntityToDocument(entity));
        }

        return documents;
    }

    public async Task<Document> UpdateAsync(Document document)
    {
        var entity = MapDocumentToTableEntity(document);
        await _tableClient.UpdateEntityAsync(entity, ETag.All);
        return document;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        try
        {
            await _tableClient.DeleteEntityAsync(id, id);
            return true;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return false;
        }
    }

    public async Task<IEnumerable<Document>> GetByStatusAsync(DocumentStatus status)
    {
        var filter = $"Status eq '{status}'";
        var entities = _tableClient.QueryAsync<DocumentTableEntity>(filter);
        var documents = new List<Document>();

        await foreach (var entity in entities)
        {
            documents.Add(MapTableEntityToDocument(entity));
        }

        return documents;
    }

    private DocumentTableEntity MapDocumentToTableEntity(Document document)
    {
        return new DocumentTableEntity
        {
            PartitionKey = document.Id,
            RowKey = document.Id,
            FileName = document.FileName,
            ContentType = document.ContentType,
            FileSize = document.FileSize,
            Status = document.Status.ToString(),
            UploadedAt = document.UploadedAt,
            VerifiedAt = document.VerifiedAt,
            VerificationResult = document.VerificationResult,
            ErrorMessage = document.ErrorMessage,
            BlobUri = document.BlobUri,
            Metadata = System.Text.Json.JsonSerializer.Serialize(document.Metadata)
        };
    }

    private Document MapTableEntityToDocument(DocumentTableEntity entity)
    {
        return new Document
        {
            Id = entity.PartitionKey,
            FileName = entity.FileName,
            ContentType = entity.ContentType,
            FileSize = entity.FileSize,
            Status = Enum.Parse<DocumentStatus>(entity.Status),
            UploadedAt = entity.UploadedAt,
            VerifiedAt = entity.VerifiedAt,
            VerificationResult = entity.VerificationResult,
            ErrorMessage = entity.ErrorMessage,
            BlobUri = entity.BlobUri,
            Metadata = string.IsNullOrEmpty(entity.Metadata) 
                ? new() 
                : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(entity.Metadata) ?? new()
        };
    }
}

public class DocumentTableEntity : ITableEntity
{
    public string PartitionKey { get; set; } = string.Empty;
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public string? VerificationResult { get; set; }
    public string? ErrorMessage { get; set; }
    public string BlobUri { get; set; } = string.Empty;
    public string? Metadata { get; set; }
}