using Azure;
using SchemeFileUploader;

namespace SchemeFileUploader.Tests;

internal sealed class FakeBlobUploader : IBlobUploader
{
    private readonly Dictionary<(string Container, string BlobName), byte[]> _blobs = new();
    private readonly List<UploadInvocation> _uploadInvocations = [];

    public IReadOnlyDictionary<(string Container, string BlobName), byte[]> Blobs => _blobs;

    public Exception? UploadException { get; set; }

    public IReadOnlyList<UploadInvocation> UploadInvocations => _uploadInvocations;

    public Task<bool> ExistsAsync(
        string container,
        string blobName,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_blobs.ContainsKey((container, blobName)));
    }

    public async Task<long> UploadAsync(
        string container,
        string blobName,
        Stream content,
        bool overwrite,
        CancellationToken cancellationToken)
    {
        _uploadInvocations.Add(new UploadInvocation(container, blobName, overwrite));

        if (UploadException is not null)
        {
            throw UploadException;
        }

        var key = (container, blobName);
        if (!overwrite && _blobs.ContainsKey(key))
        {
            throw new RequestFailedException(
                409,
                "The fake destination already contains the blob.",
                "BlobAlreadyExists",
                null);
        }

        using var destination = new MemoryStream();
        await content.CopyToAsync(destination, cancellationToken);
        var bytes = destination.ToArray();
        _blobs[key] = bytes;

        return bytes.LongLength;
    }

    internal sealed record UploadInvocation(string Container, string BlobName, bool Overwrite);
}