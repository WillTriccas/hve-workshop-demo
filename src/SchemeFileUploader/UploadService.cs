namespace SchemeFileUploader;

/// <summary>
/// Validates one local file and orchestrates its upload through an <see cref="IBlobUploader"/>.
/// </summary>
public sealed class UploadService
{
    private const string UploadCompletedMessage = "Upload completed.";
    private const string UploadRefusedMessage = "A blob with the same name already exists.";
    private const string StorageFailureMessage = "The storage upload failed.";

    private readonly IBlobUploader blobUploader;
    private readonly UploaderOptions options;

    /// <summary>
    /// Initializes a new instance of the <see cref="UploadService"/> class.
    /// </summary>
    /// <param name="blobUploader">The storage boundary used to stream content.</param>
    /// <param name="options">The validated uploader configuration.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="blobUploader"/> or <paramref name="options"/> is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="options"/> contains invalid configuration.
    /// </exception>
    public UploadService(IBlobUploader blobUploader, UploaderOptions options)
    {
        ArgumentNullException.ThrowIfNull(blobUploader);
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        this.blobUploader = blobUploader;
        this.options = options;
    }

    /// <summary>
    /// Validates and uploads one local regular file using its basename as the blob name.
    /// </summary>
    /// <param name="sourcePath">The local source path. It is never included in the result.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A sanitized result describing the upload outcome.</returns>
    /// <exception cref="OperationCanceledException">
    /// Thrown when <paramref name="cancellationToken"/> is canceled.
    /// </exception>
    public async Task<UploadResult> UploadAsync(
        string? sourcePath,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            return Failed(string.Empty, "A source file path is required.");
        }

        FileInfo sourceFile;
        long sourceLength;

        try
        {
            sourceFile = new FileInfo(sourcePath);

            if (sourceFile.LinkTarget is not null)
            {
                return Failed(string.Empty, "Symbolic links and reparse points are not supported.");
            }

            if (Directory.Exists(sourcePath))
            {
                return Failed(string.Empty, "Source path must identify a regular file.");
            }

            if (!sourceFile.Exists)
            {
                return Failed(string.Empty, "Source file was not found.");
            }

            if ((sourceFile.Attributes & FileAttributes.ReparsePoint) != 0)
            {
                return Failed(string.Empty, "Symbolic links and reparse points are not supported.");
            }

            sourceLength = sourceFile.Length;
        }
        catch (Exception exception) when (IsExpectedFileSystemFailure(exception))
        {
            return Failed(string.Empty, "Source file metadata could not be read.");
        }

        var blobName = Path.GetFileName(sourcePath);
        if (sourceLength > options.MaxFileBytes)
        {
            return Failed(blobName, "Source file exceeds the configured size limit.");
        }

        try
        {
            await using var content = new FileStream(
                sourcePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 81920,
                FileOptions.Asynchronous | FileOptions.SequentialScan);

            await blobUploader.UploadAsync(
                options.ContainerName,
                blobName,
                content,
                cancellationToken).ConfigureAwait(false);

            return new UploadResult(
                UploadOutcome.Uploaded,
                blobName,
                options.ContainerName,
                sourceLength,
                UploadCompletedMessage);
        }
        catch (BlobAlreadyExistsException)
        {
            return new UploadResult(
                UploadOutcome.Refused,
                blobName,
                options.ContainerName,
                0,
                UploadRefusedMessage);
        }
        catch (BlobUploadException)
        {
            return Failed(blobName, StorageFailureMessage);
        }
        catch (Exception exception) when (IsExpectedFileSystemFailure(exception))
        {
            return Failed(blobName, "Source file could not be read.");
        }
    }

    private static bool IsExpectedFileSystemFailure(Exception exception) =>
        exception is IOException
            or UnauthorizedAccessException
            or ArgumentException
            or NotSupportedException;

    private UploadResult Failed(string blobName, string message) =>
        new(
            UploadOutcome.Failed,
            blobName,
            options.ContainerName,
            0,
            message);
}