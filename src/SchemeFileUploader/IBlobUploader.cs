namespace SchemeFileUploader;

/// <summary>
/// Uploads readable content to a named blob destination.
/// </summary>
public interface IBlobUploader
{
    /// <summary>
    /// Uploads content to the specified container and blob name.
    /// </summary>
    /// <param name="containerName">The destination container name.</param>
    /// <param name="blobName">The server-safe destination blob name.</param>
    /// <param name="content">The readable content stream.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task that represents the asynchronous upload.</returns>
    /// <exception cref="BlobAlreadyExistsException">
    /// Thrown when the destination already contains a blob with the same name.
    /// </exception>
    /// <exception cref="BlobUploadException">
    /// Thrown when an expected storage operation fails.
    /// </exception>
    Task UploadAsync(
        string containerName,
        string blobName,
        Stream content,
        CancellationToken cancellationToken = default);
}