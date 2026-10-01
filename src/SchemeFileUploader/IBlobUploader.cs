namespace SchemeFileUploader;

/// <summary>
/// Defines asynchronous operations for querying and uploading blob content.
/// </summary>
public interface IBlobUploader
{
    /// <summary>
    /// Determines whether a blob exists in the specified container.
    /// </summary>
    /// <param name="container">The name of the container to query.</param>
    /// <param name="blobName">The name of the blob to query.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>
    /// A task whose result is <see langword="true"/> when the blob exists;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    Task<bool> ExistsAsync(
        string container,
        string blobName,
        CancellationToken cancellationToken);

    /// <summary>
    /// Uploads readable content to a blob in the specified container.
    /// </summary>
    /// <param name="container">The name of the destination container.</param>
    /// <param name="blobName">The name of the destination blob.</param>
    /// <param name="content">The readable stream containing the content to upload.</param>
    /// <param name="overwrite">
    /// <see langword="true"/> to replace an existing blob; otherwise,
    /// <see langword="false"/>.
    /// </param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task whose result is the number of bytes written.</returns>
    Task<long> UploadAsync(
        string container,
        string blobName,
        Stream content,
        bool overwrite,
        CancellationToken cancellationToken);
}