namespace DocumentAgent.Core.Interfaces;

using DocumentAgent.Core.Entities;

public interface IDocumentRepository
{
    Task<Document> CreateAsync(Document document);
    Task<Document?> GetByIdAsync(string id);
    Task<IEnumerable<Document>> GetAllAsync();
    Task<Document> UpdateAsync(Document document);
    Task<bool> DeleteAsync(string id);
    Task<IEnumerable<Document>> GetByStatusAsync(DocumentStatus status);
}