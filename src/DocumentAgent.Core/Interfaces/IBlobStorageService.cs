namespace DocumentAgent.Core.Interfaces;

public interface IBlobStorageService
{
    Task<string> UploadAsync(string containerName, string blobName, byte[] content, string contentType);
    Task<byte[]> DownloadAsync(string containerName, string blobName);
    Task<bool> DeleteAsync(string containerName, string blobName);
    Task<string> GetBlobUriAsync(string containerName, string blobName);
}