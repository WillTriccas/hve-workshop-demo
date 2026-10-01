namespace SchemeFileUploader;

/// <summary>
/// Represents the immutable result of an upload operation.
/// </summary>
/// <param name="Outcome">The outcome of the upload operation.</param>
/// <param name="BlobName">The name of the destination blob.</param>
/// <param name="Container">The name of the destination container.</param>
/// <param name="BytesWritten">The number of bytes written to blob storage.</param>
/// <param name="Message">A sanitized message describing the result.</param>
public sealed record UploadResult(
    UploadOutcome Outcome,
    string BlobName,
    string Container,
    long BytesWritten,
    string Message);