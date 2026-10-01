using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace SchemeFileUploader;

/// <summary>
/// Uploads blob content through Azure Blob Storage.
/// </summary>
public sealed class AzureBlobUploader
    : IBlobUploader
{
    private const BlobClientOptions.ServiceVersion CompatibleServiceVersion =
        BlobClientOptions.ServiceVersion.V2025_11_05;
    private readonly string _connectionString;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureBlobUploader"/> class.
    /// </summary>
    /// <param name="options">The storage configuration.</param>
    public AzureBlobUploader(UploaderOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _connectionString = options.ConnectionString;
    }

    /// <inheritdoc/>
    public async Task<bool> ExistsAsync(
        string container,
        string blobName,
        CancellationToken cancellationToken)
    {
        var containerClient = CreateContainerClient(container);
        var blobClient = containerClient.GetBlobClient(blobName);
        var response = await blobClient.ExistsAsync(cancellationToken);

        return response.Value;
    }

    /// <inheritdoc/>
    public async Task<long> UploadAsync(
        string container,
        string blobName,
        Stream content,
        bool overwrite,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);

        long? knownLength = content.CanSeek
            ? GetRemainingLength(content)
            : null;
        var countingStream = knownLength is null
            ? new CountingReadStream(content)
            : null;
        var uploadContent = countingStream ?? content;

        var containerClient = CreateContainerClient(container);
        await containerClient.CreateIfNotExistsAsync(
            PublicAccessType.None,
            cancellationToken: cancellationToken);

        var blobClient = containerClient.GetBlobClient(blobName);
        await blobClient.UploadAsync(uploadContent, overwrite, cancellationToken);

        return knownLength ?? countingStream!.BytesRead;
    }

    internal static long GetRemainingLength(Stream content) =>
        Math.Max(0, content.Length - content.Position);

    private BlobContainerClient CreateContainerClient(string container) =>
        new(
            _connectionString,
            container,
            new BlobClientOptions(CompatibleServiceVersion));
}