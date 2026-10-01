namespace SchemeFileUploader;

/// <summary>
/// Describes the stable outcome of an upload request.
/// </summary>
public enum UploadOutcome
{
    /// <summary>
    /// The source content was uploaded successfully.
    /// </summary>
    Uploaded,

    /// <summary>
    /// The upload was refused by an explicit policy such as create-only naming.
    /// </summary>
    Refused,

    /// <summary>
    /// The upload failed validation or an expected operational check.
    /// </summary>
    Failed
}