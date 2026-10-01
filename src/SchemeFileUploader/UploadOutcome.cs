namespace SchemeFileUploader;

/// <summary>
/// Identifies the outcome of an upload operation.
/// </summary>
public enum UploadOutcome
{
    /// <summary>
    /// Indicates that the content was uploaded successfully.
    /// </summary>
    Uploaded,

    /// <summary>
    /// Indicates that the upload failed.
    /// </summary>
    Failed
}