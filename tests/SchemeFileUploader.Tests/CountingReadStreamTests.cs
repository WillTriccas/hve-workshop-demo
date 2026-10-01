using SchemeFileUploader;

namespace SchemeFileUploader.Tests;

public sealed class CountingReadStreamTests
{
    [Fact]
    public async Task GivenNonSeekableSource_WhenReading_CountsBytesWithoutTakingOwnership()
    {
        // Arrange
        byte[] expectedBytes = [0, 1, 2, 3, 255];
        var source = new TrackingNonSeekableStream(expectedBytes);
        await using var sut = new CountingReadStream(source);
        using var destination = new MemoryStream();

        // Act
        await sut.CopyToAsync(destination, CancellationToken.None);
        await sut.DisposeAsync();

        // Assert
        Assert.Equal(expectedBytes.LongLength, sut.BytesRead);
        Assert.Equal(expectedBytes, destination.ToArray());
        Assert.False(source.IsDisposed);
    }

    [Fact]
    public void GivenPositionedSeekableSource_WhenMeasuring_ReturnsRemainingBytes()
    {
        // Arrange
        using var source = new MemoryStream([0, 1, 2, 3, 4]);
        source.Position = 2;

        // Act
        var result = AzureBlobUploader.GetRemainingLength(source);

        // Assert
        Assert.Equal(3, result);
    }

    private sealed class TrackingNonSeekableStream(byte[] content) : Stream
    {
        private readonly MemoryStream _innerStream = new(content);

        internal bool IsDisposed { get; private set; }

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
            IsDisposed = true;
            if (disposing)
            {
                _innerStream.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
