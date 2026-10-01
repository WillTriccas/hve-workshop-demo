using SchemeFileUploader.Cli;

namespace SchemeFileUploader.Tests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ConsoleAndEnvironmentCollection
{
    public const string Name = "Console and environment";
}

[Collection(ConsoleAndEnvironmentCollection.Name)]
public sealed class CliTests
{
    private const string ConnectionVariable = "Uploader__ConnectionString";
    private const string ContainerVariable = "Uploader__ContainerName";
    private const string MaxBytesVariable = "Uploader__MaxFileBytes";

    private static async Task<(int ExitCode, string StandardOutput, string StandardError)>
        CaptureConsoleAsync(Func<Task<int>> action)
    {
        var originalOutput = Console.Out;
        var originalError = Console.Error;
        using var output = new StringWriter();
        using var error = new StringWriter();

        try
        {
            Console.SetOut(output);
            Console.SetError(error);
            var exitCode = await action();
            return (exitCode, output.ToString(), error.ToString());
        }
        finally
        {
            Console.SetOut(originalOutput);
            Console.SetError(originalError);
        }
    }

    private static (int ExitCode, string StandardOutput, string StandardError)
        CaptureConsole(Func<int> action)
    {
        var originalOutput = Console.Out;
        var originalError = Console.Error;
        using var output = new StringWriter();
        using var error = new StringWriter();

        try
        {
            Console.SetOut(output);
            Console.SetError(error);
            var exitCode = action();
            return (exitCode, output.ToString(), error.ToString());
        }
        finally
        {
            Console.SetOut(originalOutput);
            Console.SetError(originalError);
        }
    }

    private static async Task<UploadResult> CreateResultAsync(Exception? uploaderException = null)
    {
        var directoryPath = Path.Combine(
            Path.GetTempPath(),
            $"scheme-file-uploader-cli-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directoryPath);

        try
        {
            var sourcePath = Path.Combine(directoryPath, "result.csv");
            await File.WriteAllBytesAsync(sourcePath, [1, 2, 3]);
            var uploader = new FakeBlobUploader
            {
                ExceptionToThrow = uploaderException
            };
            var options = new UploaderOptions
            {
                ConnectionString = "UseDevelopmentStorage=true",
                ContainerName = "test-container"
            };

            return await new UploadService(uploader, options).UploadAsync(sourcePath);
        }
        finally
        {
            Directory.Delete(directoryPath, recursive: true);
        }
    }

    [Fact]
    public void GivenCliContainerOverride_WhenLoadingOptions_ExpectedOverridePrecedence()
    {
        using var connection = new EnvironmentVariableScope(
            ConnectionVariable,
            "environment-connection-marker");
        using var container = new EnvironmentVariableScope(ContainerVariable, "environment-container");

        var options = Program.LoadOptions("cli-container");

        Assert.Equal("cli-container", options.ContainerName);
        Assert.Equal("environment-connection-marker", options.ConnectionString);
    }

    [Fact]
    public async Task GivenConfigurationFailure_WhenRunningCli_ExpectedExitTwoWithoutSensitiveOutput()
    {
        const string sensitiveConnection = "sensitive-connection-marker";
        var sourcePath = Path.Combine(Path.GetTempPath(), $"sensitive-path-{Guid.NewGuid():N}.csv");
        await File.WriteAllTextAsync(sourcePath, "synthetic");
        using var connection = new EnvironmentVariableScope(ConnectionVariable, sensitiveConnection);
        using var container = new EnvironmentVariableScope(ContainerVariable, "INVALID_CONTAINER");

        try
        {
            var result = await CaptureConsoleAsync(() => Program.Main([sourcePath]));

            Assert.Equal(2, result.ExitCode);
            Assert.Empty(result.StandardOutput);
            Assert.True(result.StandardError.Contains("Upload failed.", StringComparison.Ordinal));
            Assert.False(result.StandardError.Contains(sensitiveConnection, StringComparison.Ordinal));
            Assert.False(result.StandardError.Contains(sourcePath, StringComparison.Ordinal));
            Assert.False(result.StandardError.Contains(" at ", StringComparison.Ordinal));
        }
        finally
        {
            File.Delete(sourcePath);
        }
    }

    [Fact]
    public void GivenEnvironmentConfiguration_WhenLoadingOptions_ExpectedEnvironmentPrecedence()
    {
        const string environmentConnection = "environment-connection-marker";
        using var connection = new EnvironmentVariableScope(ConnectionVariable, environmentConnection);
        using var container = new EnvironmentVariableScope(ContainerVariable, "environment-container");
        using var maxBytes = new EnvironmentVariableScope(MaxBytesVariable, "2048");

        var options = Program.LoadOptions(null);

        Assert.Equal(environmentConnection, options.ConnectionString);
        Assert.Equal("environment-container", options.ContainerName);
        Assert.Equal(2048, options.MaxFileBytes);
    }

    [Fact]
    public async Task GivenFailedResult_WhenWritingResult_ExpectedExitTwoWithoutStackOrPath()
    {
        var uploadResult = await CreateResultAsync(new BlobUploadException());

        var result = CaptureConsole(() => Program.WriteResult(uploadResult));

        Assert.Equal(2, result.ExitCode);
        Assert.Empty(result.StandardOutput);
        Assert.True(result.StandardError.Contains("Upload failed.", StringComparison.Ordinal));
        Assert.False(result.StandardError.Contains(Path.GetTempPath(), StringComparison.Ordinal));
        Assert.False(result.StandardError.Contains(nameof(BlobUploadException), StringComparison.Ordinal));
        Assert.False(result.StandardError.Contains(" at ", StringComparison.Ordinal));
    }

    [Fact]
    public async Task GivenRefusedResult_WhenWritingResult_ExpectedExitOneWithoutDestinationDisclosure()
    {
        var uploadResult = await CreateResultAsync(new BlobAlreadyExistsException());

        var result = CaptureConsole(() => Program.WriteResult(uploadResult));

        Assert.Equal(1, result.ExitCode);
        Assert.Empty(result.StandardOutput);
        Assert.True(result.StandardError.Contains("Upload refused.", StringComparison.Ordinal));
        Assert.False(result.StandardError.Contains("result.csv", StringComparison.Ordinal));
        Assert.False(result.StandardError.Contains("test-container", StringComparison.Ordinal));
    }

    [Fact]
    public async Task GivenUploadedResult_WhenWritingResult_ExpectedExitZeroAndSanitizedSummary()
    {
        var uploadResult = await CreateResultAsync();

        var result = CaptureConsole(() => Program.WriteResult(uploadResult));

        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.StandardError);
        Assert.True(result.StandardOutput.Contains("Uploaded blob 'result.csv'", StringComparison.Ordinal));
        Assert.True(result.StandardOutput.Contains("container 'test-container'", StringComparison.Ordinal));
        Assert.True(result.StandardOutput.Contains("(3 bytes)", StringComparison.Ordinal));
        Assert.False(result.StandardOutput.Contains(Path.GetTempPath(), StringComparison.Ordinal));
    }

    [Fact]
    public async Task GivenUploadedResultWithControlCharacter_WhenWritingResult_ExpectedControlCharacterRedaction()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        var directoryPath = Path.Combine(
            Path.GetTempPath(),
            $"scheme-file-uploader-cli-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directoryPath);

        try
        {
            var sourcePath = Path.Combine(directoryPath, "line\nbreak.csv");
            await File.WriteAllTextAsync(sourcePath, "synthetic");
            var options = new UploaderOptions
            {
                ConnectionString = "UseDevelopmentStorage=true",
                ContainerName = "test-container"
            };
            var uploadResult = await new UploadService(new FakeBlobUploader(), options)
                .UploadAsync(sourcePath);

            var result = CaptureConsole(() => Program.WriteResult(uploadResult));

            Assert.Equal(0, result.ExitCode);
            Assert.True(result.StandardOutput.Contains("line?break.csv", StringComparison.Ordinal));
            Assert.False(result.StandardOutput.Contains("line\nbreak.csv", StringComparison.Ordinal));
        }
        finally
        {
            Directory.Delete(directoryPath, recursive: true);
        }
    }

    [Fact]
    public async Task GivenUsageError_WhenRunningCli_ExpectedExitTwoAndUsageOnly()
    {
        var result = await CaptureConsoleAsync(() => Program.Main([]));

        Assert.Equal(2, result.ExitCode);
        Assert.Empty(result.StandardOutput);
        Assert.Equal(
            $"Usage: SchemeFileUploader.Cli <local-file> [--container <name>]{Environment.NewLine}",
            result.StandardError);
    }

    private sealed class EnvironmentVariableScope : IDisposable
    {
        private readonly string name;
        private readonly string? originalValue;

        public EnvironmentVariableScope(string name, string value)
        {
            this.name = name;
            originalValue = Environment.GetEnvironmentVariable(name);
            Environment.SetEnvironmentVariable(name, value);
        }

        public void Dispose() => Environment.SetEnvironmentVariable(name, originalValue);
    }
}