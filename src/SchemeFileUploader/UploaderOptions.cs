namespace SchemeFileUploader;

/// <summary>
/// Provides configuration for uploading files to blob storage.
/// </summary>
public sealed class UploaderOptions
{
    /// <summary>
    /// Gets the connection string used to access blob storage.
    /// </summary>
    public string ConnectionString { get; init; } = "UseDevelopmentStorage=true";

    /// <summary>
    /// Gets the container used when no container is specified by the caller.
    /// </summary>
    public string DefaultContainer { get; init; } = "contributions";
}