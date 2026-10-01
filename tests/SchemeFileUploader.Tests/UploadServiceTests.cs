namespace SchemeFileUploader.Tests;

public sealed class UploadServiceTests
{
    private static UploaderOptions CreateOptions(long? maxFileBytes = null) =>
        new()
        {
            ConnectionString = "UseDevelopmentStorage=true",
            ContainerName = "test-container",
            MaxFileBytes = maxFileBytes ?? UploaderOptions.DefaultMaxFileBytes
        };

    private static string CreateTestDirectory()
    {
        var directoryPath = Path.Combine(
            Path.GetTempPath(),
            $"scheme-file-uploader-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directoryPath);
        return directoryPath;
    }

    [Fact]
    public async Task GivenCanceledToken_WhenUploadAsync_ExpectedCancellationBeforeUploaderUse()
    {
        var uploader = new FakeBlobUploader();
        var service = new UploadService(uploader, CreateOptions());
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => service.UploadAsync("unused.csv", cancellationSource.Token));
        Assert.False(uploader.WasCalled);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GivenEmptyPath_WhenUploadAsync_ExpectedSanitizedFailure(string? sourcePath)
    {
        var uploader = new FakeBlobUploader();
        var service = new UploadService(uploader, CreateOptions());

        var result = await service.UploadAsync(sourcePath);

        Assert.Equal(UploadOutcome.Failed, result.Outcome);
        Assert.Equal(0, result.BytesWritten);
        Assert.False(uploader.WasCalled);
        Assert.Equal("A source file path is required.", result.Message);
    }

    [Fact]
    public async Task GivenExistingDirectory_WhenUploadAsync_ExpectedRegularFileFailure()
    {
        var directoryPath = CreateTestDirectory();
        try
        {
            var uploader = new FakeBlobUploader();
            var service = new UploadService(uploader, CreateOptions());

            var result = await service.UploadAsync(directoryPath);

            Assert.Equal(UploadOutcome.Failed, result.Outcome);
            Assert.Equal("Source path must identify a regular file.", result.Message);
            Assert.False(uploader.WasCalled);
            Assert.False(result.Message.Contains(directoryPath, StringComparison.Ordinal));
        }
        finally
        {
            Directory.Delete(directoryPath, recursive: true);
        }
    }

    [Theory]
    [InlineData("", "test-container", 104857600)]
    [InlineData("UseDevelopmentStorage=true", "Invalid_Container", 104857600)]
    [InlineData("UseDevelopmentStorage=true", "test-container", 0)]
    public void GivenInvalidConfiguration_WhenConstructingService_ExpectedSanitizedFailure(
        string connectionString,
        string containerName,
        long maxFileBytes)
    {
        var options = new UploaderOptions
        {
            ConnectionString = connectionString,
            ContainerName = containerName,
            MaxFileBytes = maxFileBytes
        };

        var exception = Assert.Throws<InvalidOperationException>(
            () => new UploadService(new FakeBlobUploader(), options));

        if (connectionString.Length > 0)
        {
            Assert.False(exception.Message.Contains(connectionString, StringComparison.Ordinal));
        }

        Assert.False(exception.Message.Contains(containerName, StringComparison.Ordinal));
    }

    [Fact]
    public async Task GivenMissingFile_WhenUploadAsync_ExpectedNotFoundFailureWithoutPathDisclosure()
    {
        var directoryPath = CreateTestDirectory();
        try
        {
            var sourcePath = Path.Combine(directoryPath, "missing.csv");
            var uploader = new FakeBlobUploader();
            var service = new UploadService(uploader, CreateOptions());

            var result = await service.UploadAsync(sourcePath);

            Assert.Equal(UploadOutcome.Failed, result.Outcome);
            Assert.Equal("Source file was not found.", result.Message);
            Assert.False(uploader.WasCalled);
            Assert.False(result.Message.Contains(sourcePath, StringComparison.Ordinal));
        }
        finally
        {
            Directory.Delete(directoryPath, recursive: true);
        }
    }

    [Fact]
    public async Task GivenOversizedFile_WhenUploadAsync_ExpectedRejectionBeforeUploaderUse()
    {
        var directoryPath = CreateTestDirectory();
        var sourcePath = Path.Combine(directoryPath, "oversized.csv");
        try
        {
            await using (var source = File.Create(sourcePath))
            {
                source.SetLength(UploaderOptions.DefaultMaxFileBytes + 1);
            }

            if (!OperatingSystem.IsWindows())
            {
                File.SetUnixFileMode(sourcePath, UnixFileMode.UserWrite);
            }

            var uploader = new FakeBlobUploader();
            var service = new UploadService(uploader, CreateOptions());

            var result = await service.UploadAsync(sourcePath);

            Assert.Equal(UploadOutcome.Failed, result.Outcome);
            Assert.Equal("Source file exceeds the configured size limit.", result.Message);
            Assert.Equal(0, result.BytesWritten);
            Assert.False(uploader.WasCalled);
        }
        finally
        {
            if (File.Exists(sourcePath) && !OperatingSystem.IsWindows())
            {
                File.SetUnixFileMode(
                    sourcePath,
                    UnixFileMode.UserRead | UnixFileMode.UserWrite);
            }

            Directory.Delete(directoryPath, recursive: true);
        }
    }

    [Fact]
    public async Task GivenStorageCollision_WhenUploadAsync_ExpectedRefusal()
    {
        var directoryPath = CreateTestDirectory();
        try
        {
            var sourcePath = Path.Combine(directoryPath, "collision.csv");
            await File.WriteAllTextAsync(sourcePath, "synthetic");
            var uploader = new FakeBlobUploader
            {
                ExceptionToThrow = new BlobAlreadyExistsException()
            };
            var service = new UploadService(uploader, CreateOptions());

            var result = await service.UploadAsync(sourcePath);

            Assert.Equal(UploadOutcome.Refused, result.Outcome);
            Assert.Equal("collision.csv", result.BlobName);
            Assert.Equal(0, result.BytesWritten);
            Assert.Equal("A blob with the same name already exists.", result.Message);
        }
        finally
        {
            Directory.Delete(directoryPath, recursive: true);
        }
    }

    [Fact]
    public async Task GivenStorageFailure_WhenUploadAsync_ExpectedSanitizedFailure()
    {
        var directoryPath = CreateTestDirectory();
        try
        {
            var sourcePath = Path.Combine(directoryPath, "failure.csv");
            await File.WriteAllTextAsync(sourcePath, "synthetic");
            var uploader = new FakeBlobUploader
            {
                ExceptionToThrow = new BlobUploadException()
            };
            var service = new UploadService(uploader, CreateOptions());

            var result = await service.UploadAsync(sourcePath);

            Assert.Equal(UploadOutcome.Failed, result.Outcome);
            Assert.Equal("The storage upload failed.", result.Message);
            Assert.Equal(0, result.BytesWritten);
            Assert.False(result.Message.Contains(sourcePath, StringComparison.Ordinal));
        }
        finally
        {
            Directory.Delete(directoryPath, recursive: true);
        }
    }

    [Fact]
    public async Task GivenSupportedSymbolicLink_WhenUploadAsync_ExpectedPolicyRejection()
    {
        var directoryPath = CreateTestDirectory();
        try
        {
            var targetPath = Path.Combine(directoryPath, "target.csv");
            var linkPath = Path.Combine(directoryPath, "link.csv");
            await File.WriteAllTextAsync(targetPath, "synthetic");

            try
            {
                File.CreateSymbolicLink(linkPath, targetPath);
            }
            catch (Exception exception) when (
                exception is IOException
                    or UnauthorizedAccessException
                    or PlatformNotSupportedException)
            {
                return;
            }

            var uploader = new FakeBlobUploader();
            var service = new UploadService(uploader, CreateOptions());

            var result = await service.UploadAsync(linkPath);

            Assert.Equal(UploadOutcome.Failed, result.Outcome);
            Assert.Equal("Symbolic links and reparse points are not supported.", result.Message);
            Assert.False(uploader.WasCalled);
        }
        finally
        {
            Directory.Delete(directoryPath, recursive: true);
        }
    }

    [Fact]
    public async Task GivenValidFile_WhenUploadAsync_ExpectedExactBytesBasenameAndCount()
    {
        var directoryPath = CreateTestDirectory();
        try
        {
            byte[] expectedBytes = [0, 1, 2, 10, 13, 255];
            var sourcePath = Path.Combine(directoryPath, "known-bytes.csv");
            await File.WriteAllBytesAsync(sourcePath, expectedBytes);
            var uploader = new FakeBlobUploader();
            var service = new UploadService(uploader, CreateOptions());

            var result = await service.UploadAsync(sourcePath);

            Assert.Equal(UploadOutcome.Uploaded, result.Outcome);
            Assert.Equal("known-bytes.csv", result.BlobName);
            Assert.Equal("test-container", result.ContainerName);
            Assert.Equal(expectedBytes.Length, result.BytesWritten);
            Assert.Equal(expectedBytes, uploader.UploadedBytes);
            Assert.Equal("known-bytes.csv", uploader.BlobName);
            Assert.Equal("test-container", uploader.ContainerName);
            Assert.False(result.Message.Contains(directoryPath, StringComparison.Ordinal));
        }
        finally
        {
            Directory.Delete(directoryPath, recursive: true);
        }
    }
}