namespace SchemeFileUploader;

/// <summary>
/// Represents a sanitized, immutable upload result.
/// </summary>
public sealed record UploadResult
{
    internal UploadResult(
        UploadOutcome outcome,
        string blobName,
        string containerName,
        long bytesWritten,
        string message)
    {
        Outcome = outcome;
        BlobName = blobName;
        ContainerName = containerName;
        BytesWritten = bytesWritten;
        Message = message;
    }

    /// <summary>
    /// Gets the stable upload outcome.
    /// </summary>
    public UploadOutcome Outcome { get; }

    /// <summary>
    /// Gets the destination blob name without any local path information.
    /// </summary>
    public string BlobName { get; }

    /// <summary>
    /// Gets the destination container name.
    /// </summary>
    public string ContainerName { get; }

    /// <summary>
    /// Gets the number of source bytes uploaded, or zero when no upload completed.
    /// </summary>
    public long BytesWritten { get; }

    /// <summary>
    /// Gets a sanitized message suitable for expected command-line output.
    /// </summary>
    public string Message { get; }
}