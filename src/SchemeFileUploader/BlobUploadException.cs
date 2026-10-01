namespace SchemeFileUploader;

/// <summary>
/// Represents an expected storage failure without exposing provider exception details.
/// </summary>
public sealed class BlobUploadException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BlobUploadException"/> class.
    /// </summary>
    public BlobUploadException()
        : base("The storage operation failed.")
    {
    }
}