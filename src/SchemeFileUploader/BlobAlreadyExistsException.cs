namespace SchemeFileUploader;

/// <summary>
/// Represents a create-only upload refused because the destination name already exists.
/// </summary>
public sealed class BlobAlreadyExistsException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BlobAlreadyExistsException"/> class.
    /// </summary>
    public BlobAlreadyExistsException()
        : base("The destination blob already exists.")
    {
    }
}