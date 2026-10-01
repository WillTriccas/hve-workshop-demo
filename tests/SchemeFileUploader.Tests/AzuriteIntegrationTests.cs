using Azure;
using Azure.Storage.Blobs;
using SchemeFileUploader;

namespace SchemeFileUploader.Tests;

public sealed class AzuriteIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task GivenPositionedAndNonSeekableStreams_WhenUploading_ReturnsConsumedBytes()
    {
        // Arrange
        const string connectionString = "UseDevelopmentStorage=true";
        byte[] sourceBytes = [0, 1, 2, 3, 255];
        var containerName = $"integration-{Guid.NewGuid():N}";
        var clientOptions = new BlobClientOptions(
            BlobClientOptions.ServiceVersion.V2025_11_05);
        var containerClient = new BlobContainerClient(
            connectionString,
            containerName,
            clientOptions);
        var sut = new AzureBlobUploader(new UploaderOptions
        {
            ConnectionString = connectionString
        });

        try
        {
            await using var positionedContent = new MemoryStream(sourceBytes);
            positionedContent.Position = 2;
            await using var nonSeekableContent = new NonSeekableReadStream(sourceBytes);

            // Act
            var positionedBytesWritten = await sut.UploadAsync(
                containerName,
                "positioned.bin",
                positionedContent,
                overwrite: false,
                CancellationToken.None);
            var nonSeekableBytesWritten = await sut.UploadAsync(
                containerName,
                "non-seekable.bin",
                nonSeekableContent,
                overwrite: false,
                CancellationToken.None);

            // Assert
            Assert.Equal(3, positionedBytesWritten);
            Assert.Equal(sourceBytes.LongLength, nonSeekableBytesWritten);
        }
        finally
        {
            await containerClient.DeleteIfExistsAsync(cancellationToken: CancellationToken.None);
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GivenExistingBlob_WhenUploadingWithoutOverwrite_PreservesOriginalBytes()
    {
        // Arrange
        const string connectionString = "UseDevelopmentStorage=true";
        const string blobName = "round-trip.bin";
        byte[] expectedBytes = [0, 1, 2, 3, 255];
        byte[] replacementBytes = [5, 6, 7];
        var containerName = $"integration-{Guid.NewGuid():N}";
        var clientOptions = new BlobClientOptions(
            BlobClientOptions.ServiceVersion.V2025_11_05);
        var containerClient = new BlobContainerClient(
            connectionString,
            containerName,
            clientOptions);
        var sut = new AzureBlobUploader(new UploaderOptions
        {
            ConnectionString = connectionString
        });

        try
        {
            await using var content = new MemoryStream(expectedBytes);

            // Act
            var bytesWritten = await sut.UploadAsync(
                containerName,
                blobName,
                content,
                overwrite: false,
                CancellationToken.None);
            await using var replacementContent = new MemoryStream(replacementBytes);
            var exception = await Assert.ThrowsAsync<RequestFailedException>(
                () => sut.UploadAsync(
                    containerName,
                    blobName,
                    replacementContent,
                    overwrite: false,
                    CancellationToken.None));
            var download = await containerClient
                .GetBlobClient(blobName)
                .DownloadContentAsync(CancellationToken.None);

            // Assert
            Assert.Equal(expectedBytes.LongLength, bytesWritten);
            Assert.Equal(409, exception.Status);
            Assert.Equal(expectedBytes, download.Value.Content.ToArray());
        }
        finally
        {
            await containerClient.DeleteIfExistsAsync(cancellationToken: CancellationToken.None);
        }
    }

    private sealed class NonSeekableReadStream(byte[] content) : Stream
    {
        private readonly MemoryStream _innerStream = new(content);

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush() => throw new NotSupportedException();

        public override int Read(byte[] buffer, int offset, int count) =>
            _innerStream.Read(buffer, offset, count);

        public override int Read(Span<byte> buffer) => _innerStream.Read(buffer);

        public override Task<int> ReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken) =>
            _innerStream.ReadAsync(buffer, offset, count, cancellationToken);

        public override ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default) =>
            _innerStream.ReadAsync(buffer, cancellationToken);

        public override long Seek(long offset, SeekOrigin origin) =>
            throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) =>
            throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _innerStream.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
