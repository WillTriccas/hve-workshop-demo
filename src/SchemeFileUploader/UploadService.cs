using Azure;

namespace SchemeFileUploader;

/// <summary>
/// Validates local upload sources and coordinates blob uploads.
/// </summary>
/// <param name="blobUploader">The blob storage abstraction.</param>
public sealed class UploadService(IBlobUploader blobUploader)
{
    private readonly IBlobUploader _blobUploader = blobUploader
        ?? throw new ArgumentNullException(nameof(blobUploader));

    /// <summary>
    /// Uploads a local file without replacing an existing destination blob.
    /// </summary>
    /// <param name="sourcePath">The path of the local file to upload.</param>
    /// <param name="container">The destination container name.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A sanitized result describing the upload outcome.</returns>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is canceled.
    /// </exception>
    public async Task<UploadResult> UploadAsync(
        string? sourcePath,
        string container,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            return Failure(string.Empty, container, "A source file path is required.");
        }

        var blobName = Path.GetFileName(sourcePath);
        if (string.IsNullOrEmpty(blobName))
        {
            return Failure(string.Empty, container, "The source path must identify a file.");
        }

        if (!File.Exists(sourcePath))
        {
            return Failure(blobName, container, $"Source file '{blobName}' was not found.");
        }

        try
        {
            await using var content = File.OpenRead(sourcePath);
            var bytesWritten = await _blobUploader.UploadAsync(
                container,
                blobName,
                content,
                overwrite: false,
                cancellationToken);

            return new UploadResult(
                UploadOutcome.Uploaded,
                blobName,
                container,
                bytesWritten,
                FormattableString.Invariant(
                    $"Uploaded {blobName} to {container} ({bytesWritten:N0} bytes)"));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (UnauthorizedAccessException)
        {
            return Failure(blobName, container, $"Source file '{blobName}' could not be read.");
        }
        catch (IOException)
        {
            return Failure(blobName, container, $"Source file '{blobName}' could not be read.");
        }
        catch (RequestFailedException exception) when (exception.Status == 409)
        {
            return Failure(
                blobName,
                container,
                $"Container '{container}' already contains blob '{blobName}'; collision handling is not supported.");
        }
        catch (RequestFailedException)
        {
            return Failure(
                blobName,
                container,
                $"Upload of '{blobName}' to container '{container}' failed. Verify that storage is available and try again.");
        }
    }

    private static UploadResult Failure(string blobName, string container, string message) =>
        new(UploadOutcome.Failed, blobName, container, 0, message);
}