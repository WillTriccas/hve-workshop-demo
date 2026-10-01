using Azure;
using SchemeFileUploader;

namespace SchemeFileUploader.Tests;

public sealed class UploadServiceTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GivenBlankSource_WhenUploading_ReturnsFailureWithoutInvokingUploader(
        string? sourcePath)
    {
        // Arrange
        var uploader = new FakeBlobUploader();
        var sut = new UploadService(uploader);

        // Act
        var result = await sut.UploadAsync(sourcePath, "incoming", CancellationToken.None);

        // Assert
        Assert.Equal(UploadOutcome.Failed, result.Outcome);
        Assert.Equal(0, result.BytesWritten);
        Assert.Contains("required", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(uploader.UploadInvocations);
    }

    [Fact]
    public async Task GivenCanceledUpload_WhenUploading_PropagatesCancellation()
    {
        // Arrange
        var sourcePath = Path.GetTempFileName();
        await File.WriteAllTextAsync(sourcePath, "content");
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var uploader = new FakeBlobUploader();
        var sut = new UploadService(uploader);

        try
        {
            // Act & Assert
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => sut.UploadAsync(sourcePath, "incoming", cancellationSource.Token));
        }
        finally
        {
            File.Delete(sourcePath);
        }
    }

    [Fact]
    public async Task GivenEmptyDestination_WhenUploading_ReturnsUploadedResultAndStoresExactBytes()
    {
        // Arrange
        byte[] expectedBytes = [0, 1, 2, 3, 255];
        var sourcePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.csv");
        await File.WriteAllBytesAsync(sourcePath, expectedBytes);
        var uploader = new FakeBlobUploader();
        var sut = new UploadService(uploader);

        try
        {
            // Act
            var result = await sut.UploadAsync(sourcePath, "incoming", CancellationToken.None);

            // Assert
            Assert.Equal(UploadOutcome.Uploaded, result.Outcome);
            Assert.Equal(Path.GetFileName(sourcePath), result.BlobName);
            Assert.Equal("incoming", result.Container);
            Assert.Equal(expectedBytes.LongLength, result.BytesWritten);
            Assert.Contains(result.BlobName, result.Message, StringComparison.Ordinal);
            Assert.Contains(result.Container, result.Message, StringComparison.Ordinal);
            Assert.Equal(
                expectedBytes,
                uploader.Blobs[(result.Container, result.BlobName)]);
        }
        finally
        {
            File.Delete(sourcePath);
        }
    }

    [Fact]
    public async Task GivenExistingDestination_WhenUploading_ReturnsSanitizedCollisionFailure()
    {
        // Arrange
        var sourcePath = Path.GetTempFileName();
        await File.WriteAllTextAsync(sourcePath, "content");
        var uploader = new FakeBlobUploader();
        var sut = new UploadService(uploader);

        try
        {
            await sut.UploadAsync(sourcePath, "incoming", CancellationToken.None);

            // Act
            var result = await sut.UploadAsync(sourcePath, "incoming", CancellationToken.None);

            // Assert
            Assert.Equal(UploadOutcome.Failed, result.Outcome);
            Assert.Equal(0, result.BytesWritten);
            Assert.Contains("already contains", result.Message, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("BlobAlreadyExists", result.Message, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(sourcePath);
        }
    }

    [Fact]
    public async Task GivenMissingSource_WhenUploading_ReturnsFailureWithoutInvokingUploader()
    {
        // Arrange
        var sourcePath = Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.csv");
        var uploader = new FakeBlobUploader();
        var sut = new UploadService(uploader);

        // Act
        var result = await sut.UploadAsync(sourcePath, "incoming", CancellationToken.None);

        // Assert
        Assert.Equal(UploadOutcome.Failed, result.Outcome);
        Assert.Equal(0, result.BytesWritten);
        Assert.Contains("not found", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(uploader.UploadInvocations);
    }

    [Fact]
    public async Task GivenReadableSource_WhenUploading_RequestsNonOverwritingUpload()
    {
        // Arrange
        var sourcePath = Path.GetTempFileName();
        await File.WriteAllTextAsync(sourcePath, "content");
        var uploader = new FakeBlobUploader();
        var sut = new UploadService(uploader);

        try
        {
            // Act
            await sut.UploadAsync(sourcePath, "incoming", CancellationToken.None);

            // Assert
            var invocation = Assert.Single(uploader.UploadInvocations);
            Assert.False(invocation.Overwrite);
        }
        finally
        {
            File.Delete(sourcePath);
        }
    }

    [Fact]
    public async Task GivenStorageFailureContainingSensitiveDetails_WhenUploading_ReturnsSanitizedFailure()
    {
        // Arrange
        const string sensitiveDetails = "AccountKey=super-secret";
        var sourcePath = Path.GetTempFileName();
        await File.WriteAllTextAsync(sourcePath, "content");
        var uploader = new FakeBlobUploader
        {
            UploadException = new RequestFailedException(sensitiveDetails)
        };
        var sut = new UploadService(uploader);

        try
        {
            // Act
            var result = await sut.UploadAsync(sourcePath, "incoming", CancellationToken.None);

            // Assert
            Assert.Equal(UploadOutcome.Failed, result.Outcome);
            Assert.Equal(0, result.BytesWritten);
            Assert.DoesNotContain(sensitiveDetails, result.Message, StringComparison.Ordinal);
            Assert.Contains("storage", result.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            File.Delete(sourcePath);
        }
    }
}