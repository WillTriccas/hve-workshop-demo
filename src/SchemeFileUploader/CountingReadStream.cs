namespace SchemeFileUploader;

internal sealed class CountingReadStream(Stream innerStream) : Stream
{
    private readonly Stream _innerStream = innerStream
        ?? throw new ArgumentNullException(nameof(innerStream));

    internal long BytesRead { get; private set; }

    public override bool CanRead => _innerStream.CanRead;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override void Flush() => _innerStream.Flush();

    public override int Read(byte[] buffer, int offset, int count)
    {
        var bytesRead = _innerStream.Read(buffer, offset, count);
        BytesRead += bytesRead;
        return bytesRead;
    }

    public override int Read(Span<byte> buffer)
    {
        var bytesRead = _innerStream.Read(buffer);
        BytesRead += bytesRead;
        return bytesRead;
    }

    public override async Task<int> ReadAsync(
        byte[] buffer,
        int offset,
        int count,
        CancellationToken cancellationToken)
    {
        var bytesRead = await _innerStream.ReadAsync(
            buffer,
            offset,
            count,
            cancellationToken);
        BytesRead += bytesRead;
        return bytesRead;
    }

    public override async ValueTask<int> ReadAsync(
        Memory<byte> buffer,
        CancellationToken cancellationToken = default)
    {
        var bytesRead = await _innerStream.ReadAsync(buffer, cancellationToken);
        BytesRead += bytesRead;
        return bytesRead;
    }

    public override int ReadByte()
    {
        var value = _innerStream.ReadByte();
        if (value >= 0)
        {
            BytesRead++;
        }

        return value;
    }

    public override long Seek(long offset, SeekOrigin origin) =>
        throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) =>
        throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
    }

    public override ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}
