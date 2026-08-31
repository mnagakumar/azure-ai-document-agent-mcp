namespace DocumentAgent.Infrastructure.Azure;

using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using DocumentAgent.Core.Interfaces;

public class AzureBlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;

    public AzureBlobStorageService(BlobServiceClient blobServiceClient)
    {
        _blobServiceClient = blobServiceClient;
    }

    public async Task<string> UploadAsync(string containerName, string blobName, byte[] content, string contentType)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        await containerClient.CreateIfNotExistsAsync();

        var blobClient = containerClient.GetBlobClient(blobName);
        using (var stream = new MemoryStream(content))
        {
            await blobClient.UploadAsync(stream, overwrite: true);
        }

        // Set content type
        await blobClient.SetHttpHeadersAsync(new BlobHttpHeaders { ContentType = contentType });

        return blobClient.Uri.AbsoluteUri;
    }

    public async Task<byte[]> DownloadAsync(string containerName, string blobName)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);

        var download = await blobClient.DownloadAsync();
        using (var stream = new MemoryStream())
        {
            await download.Value.Content.CopyToAsync(stream);
            return stream.ToArray();
        }
    }

    public async Task<bool> DeleteAsync(string containerName, string blobName)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);
        var result = await blobClient.DeleteIfExistsAsync();
        return result.Value;
    }

    public async Task<string> GetBlobUriAsync(string containerName, string blobName)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);
        return await Task.FromResult(blobClient.Uri.AbsoluteUri);
    }
}