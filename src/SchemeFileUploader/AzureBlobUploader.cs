using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace SchemeFileUploader;

/// <summary>
/// Uploads streams to Azure Blob Storage with atomic create-only semantics.
/// </summary>
public sealed class AzureBlobUploader : IBlobUploader
{
    private readonly BlobServiceClient blobServiceClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureBlobUploader"/> class.
    /// </summary>
    /// <param name="connectionString">The Blob Storage connection string.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="connectionString"/> is empty or whitespace.
    /// </exception>
    /// <exception cref="BlobUploadException">
    /// Thrown when the connection configuration cannot initialize the client.
    /// </exception>
    public AzureBlobUploader(string connectionString)
        : this(CreateBlobServiceClient(connectionString))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureBlobUploader"/> class.
    /// </summary>
    /// <param name="blobServiceClient">The Blob Storage service client.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="blobServiceClient"/> is null.
    /// </exception>
    public AzureBlobUploader(BlobServiceClient blobServiceClient)
    {
        ArgumentNullException.ThrowIfNull(blobServiceClient);
        this.blobServiceClient = blobServiceClient;
    }

    /// <inheritdoc/>
    public async Task UploadAsync(
        string containerName,
        string blobName,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(containerName);
        ArgumentException.ThrowIfNullOrWhiteSpace(blobName);
        ArgumentNullException.ThrowIfNull(content);

        try
        {
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync(
                PublicAccessType.None,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            var blobClient = containerClient.GetBlobClient(blobName);
            var uploadOptions = new BlobUploadOptions
            {
                Conditions = new BlobRequestConditions
                {
                    IfNoneMatch = ETag.All
                }
            };

            await blobClient.UploadAsync(content, uploadOptions, cancellationToken).ConfigureAwait(false);
        }
        catch (RequestFailedException exception) when (IsExistingBlobResponse(exception))
        {
            throw new BlobAlreadyExistsException();
        }
        catch (RequestFailedException)
        {
            throw new BlobUploadException();
        }
    }

    private static BlobServiceClient CreateBlobServiceClient(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        try
        {
            var clientOptions = new BlobClientOptions(
                BlobClientOptions.ServiceVersion.V2025_11_05);
            return new BlobServiceClient(connectionString, clientOptions);
        }
        catch (Exception exception) when (exception is ArgumentException or FormatException)
        {
            throw new BlobUploadException();
        }
    }

    private static bool IsExistingBlobResponse(RequestFailedException exception) =>
        exception.Status == 409
            && string.Equals(
                exception.ErrorCode,
                BlobErrorCode.BlobAlreadyExists.ToString(),
                StringComparison.Ordinal)
        || exception.Status == 412
            && string.Equals(
                exception.ErrorCode,
                BlobErrorCode.ConditionNotMet.ToString(),
                StringComparison.Ordinal);
}