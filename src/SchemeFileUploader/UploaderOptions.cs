namespace SchemeFileUploader;

/// <summary>
/// Provides validated storage destination and source-size settings.
/// </summary>
public sealed class UploaderOptions
{
    /// <summary>
    /// The default maximum source-file size of 100 MiB.
    /// </summary>
    public const long DefaultMaxFileBytes = 104857600;

    /// <summary>
    /// Gets or sets the storage connection string.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the destination container name.
    /// </summary>
    public string ContainerName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the maximum accepted source-file size in bytes.
    /// </summary>
    public long MaxFileBytes { get; set; } = DefaultMaxFileBytes;

    /// <summary>
    /// Validates the configuration without including configured values in error messages.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a required setting is absent or invalid.
    /// </exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
        {
            throw new InvalidOperationException("Uploader connection configuration is required.");
        }

        if (!IsValidContainerName(ContainerName))
        {
            throw new InvalidOperationException("Uploader container configuration is invalid.");
        }

        if (MaxFileBytes <= 0)
        {
            throw new InvalidOperationException("Uploader maximum file size must be greater than zero.");
        }
    }

    private static bool IsValidContainerName(string containerName)
    {
        if (containerName.Length is < 3 or > 63
            || !IsLowercaseLetterOrDigit(containerName[0])
            || !IsLowercaseLetterOrDigit(containerName[^1]))
        {
            return false;
        }

        var previousWasHyphen = false;
        foreach (var character in containerName)
        {
            if (character == '-')
            {
                if (previousWasHyphen)
                {
                    return false;
                }

                previousWasHyphen = true;
                continue;
            }

            if (!IsLowercaseLetterOrDigit(character))
            {
                return false;
            }

            previousWasHyphen = false;
        }

        return true;
    }

    private static bool IsLowercaseLetterOrDigit(char character) =>
        character is >= 'a' and <= 'z' or >= '0' and <= '9';
}