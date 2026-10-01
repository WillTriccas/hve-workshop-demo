namespace SchemeFileUploader.Tests;

internal sealed class FakeBlobUploader : IBlobUploader
{
    public string? BlobName { get; private set; }

    public string? ContainerName { get; private set; }

    public Exception? ExceptionToThrow { get; set; }

    public byte[] UploadedBytes { get; private set; } = [];

    public bool WasCalled { get; private set; }

    public async Task UploadAsync(
        string containerName,
        string blobName,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        WasCalled = true;
        ContainerName = containerName;
        BlobName = blobName;

        if (ExceptionToThrow is not null)
        {
            throw ExceptionToThrow;
        }

        await using var uploadedContent = new MemoryStream();
        await content.CopyToAsync(uploadedContent, cancellationToken);
        UploadedBytes = uploadedContent.ToArray();
    }
}