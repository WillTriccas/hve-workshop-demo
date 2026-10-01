using SchemeFileUploader;
using SchemeFileUploader.Cli;

namespace SchemeFileUploader.Tests;

public sealed class ProgramTests
{
    private const string SensitiveValue = "AccountKey=sentinel-secret";

    [Fact]
    public async Task GivenContainerOverride_WhenRunning_UsesOverrideAndReturnsZero()
    {
        // Arrange
        var sourcePath = await CreateSourceFileAsync();
        var uploader = new FakeBlobUploader();

        try
        {
            // Act
            var result = await RunAsync(
                [sourcePath, "--container", "override"],
                () => ValidOptions(),
                _ => uploader);

            // Assert
            Assert.Equal(0, result.ExitCode);
            var invocation = Assert.Single(uploader.UploadInvocations);
            Assert.Equal("override", invocation.Container);
        }
        finally
        {
            File.Delete(sourcePath);
        }
    }

    [Fact]
    public async Task GivenDefaultContainer_WhenRunning_UploadsAndReportsResult()
    {
        // Arrange
        var sourcePath = await CreateSourceFileAsync();
        var uploader = new FakeBlobUploader();

        try
        {
            // Act
            var result = await RunAsync(
                [sourcePath],
                () => ValidOptions(),
                _ => uploader);

            // Assert
            Assert.Equal(0, result.ExitCode);
            var invocation = Assert.Single(uploader.UploadInvocations);
            Assert.Equal("default-container", invocation.Container);
            Assert.Contains(Path.GetFileName(sourcePath), result.StandardOutput, StringComparison.Ordinal);
            Assert.Contains("default-container", result.StandardOutput, StringComparison.Ordinal);
            Assert.Contains("7 bytes", result.StandardOutput, StringComparison.Ordinal);
            Assert.Equal(string.Empty, result.StandardError);
        }
        finally
        {
            File.Delete(sourcePath);
        }
    }

    [Theory]
    [InlineData("ConnectionString")]
    [InlineData("DefaultContainer")]
    public async Task GivenEmptyRequiredSetting_WhenLoadingConfiguration_ReturnsTwoWithoutValue(
        string settingName)
    {
        // Arrange
        var configurationPath = await CreateConfigurationFileAsync(
            settingName == "ConnectionString" ? string.Empty : SensitiveValue,
            settingName == "DefaultContainer" ? string.Empty : "default-container");

        try
        {
            // Act
            var result = await RunAsync(
                ["unused.csv"],
                () => Program.LoadOptions(configurationPath));

            // Assert
            Assert.Equal(2, result.ExitCode);
            Assert.Contains(settingName, result.StandardError, StringComparison.Ordinal);
            Assert.DoesNotContain(SensitiveValue, result.StandardOutput, StringComparison.Ordinal);
            Assert.DoesNotContain(SensitiveValue, result.StandardError, StringComparison.Ordinal);
            Assert.DoesNotContain(" at ", result.StandardError, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(configurationPath);
        }
    }

    [Fact]
    public async Task GivenMalformedConfiguration_WhenRunning_ReturnsTwoWithoutRawJsonError()
    {
        // Arrange
        var configurationPath = Path.GetTempFileName();
        await File.WriteAllTextAsync(configurationPath, $"{{ invalid {SensitiveValue}");

        try
        {
            // Act
            var result = await RunAsync(
                ["unused.csv"],
                () => Program.LoadOptions(configurationPath));

            // Assert
            Assert.Equal(2, result.ExitCode);
            Assert.Contains("malformed JSON", result.StandardError, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(SensitiveValue, result.StandardOutput, StringComparison.Ordinal);
            Assert.DoesNotContain(SensitiveValue, result.StandardError, StringComparison.Ordinal);
            Assert.DoesNotContain("JsonReaderException", result.StandardError, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(configurationPath);
        }
    }

    [Fact]
    public async Task GivenMissingConfiguration_WhenRunning_ReturnsTwoWithoutExceptionDetails()
    {
        // Arrange
        var configurationPath = Path.Combine(
            Path.GetTempPath(),
            $"missing-{Guid.NewGuid():N}.json");

        // Act
        var result = await RunAsync(
            ["unused.csv"],
            () => Program.LoadOptions(configurationPath));

        // Assert
        Assert.Equal(2, result.ExitCode);
        Assert.Contains("appsettings.json", result.StandardError, StringComparison.Ordinal);
        Assert.DoesNotContain(configurationPath, result.StandardError, StringComparison.Ordinal);
        Assert.DoesNotContain("FileNotFoundException", result.StandardError, StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(InvalidArguments))]
    public async Task GivenInvalidArguments_WhenRunning_ReturnsTwoWithoutLoadingConfiguration(
        string[] arguments,
        string expectedMessage)
    {
        // Arrange
        var optionsLoaded = false;
        var factoryInvoked = false;

        // Act
        var result = await RunAsync(
            arguments,
            () =>
            {
                optionsLoaded = true;
                return ValidOptions();
            },
            _ =>
            {
                factoryInvoked = true;
                return new FakeBlobUploader();
            });

        // Assert
        Assert.Equal(2, result.ExitCode);
        Assert.Contains(expectedMessage, result.StandardError, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(
            "Usage: uploader <file> [--container <name>]",
            result.StandardError,
            StringComparison.Ordinal);
        Assert.False(optionsLoaded);
        Assert.False(factoryInvoked);
    }

    [Fact]
    public async Task GivenMissingSource_WhenRunning_ReturnsTwoWithoutStorageOperation()
    {
        // Arrange
        var sourcePath = Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.csv");
        var uploader = new FakeBlobUploader();

        // Act
        var result = await RunAsync(
            [sourcePath],
            () => ValidOptions(),
            _ => uploader);

        // Assert
        Assert.Equal(2, result.ExitCode);
        Assert.Contains("not found", result.StandardError, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(result.StandardOutput);
        Assert.Empty(uploader.UploadInvocations);
    }

    [Fact]
    public async Task GivenUnexpectedSensitiveException_WhenRunning_RedactsOutputAndReturnsTwo()
    {
        // Arrange
        var sourcePath = await CreateSourceFileAsync();

        try
        {
            // Act
            var result = await RunAsync(
                [sourcePath],
                () => ValidOptions(),
                _ => throw new InvalidOperationException(
                    $"{SensitiveValue} http://127.0.0.1:10000/request"));

            // Assert
            Assert.Equal(2, result.ExitCode);
            Assert.DoesNotContain(SensitiveValue, result.StandardOutput, StringComparison.Ordinal);
            Assert.DoesNotContain(SensitiveValue, result.StandardError, StringComparison.Ordinal);
            Assert.DoesNotContain("127.0.0.1", result.StandardError, StringComparison.Ordinal);
            Assert.DoesNotContain("InvalidOperationException", result.StandardError, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(sourcePath);
        }
    }

    public static TheoryData<string[], string> InvalidArguments => new()
    {
        { [], "source file path" },
        { ["input.csv", "--container"], "requires a container name" },
        { ["input.csv", "--unknown"], "unknown option" },
        { ["input.csv", "--container", "one", "--container", "two"], "only once" },
        { ["input.csv", "extra.csv"], "only one source" }
    };

    private static async Task<string> CreateConfigurationFileAsync(
        string connectionString,
        string defaultContainer)
    {
        var configurationPath = Path.GetTempFileName();
        await File.WriteAllTextAsync(
            configurationPath,
            $$"""
            {
              "Uploader": {
                "ConnectionString": "{{connectionString}}",
                "DefaultContainer": "{{defaultContainer}}"
              }
            }
            """);
        return configurationPath;
    }

    private static async Task<string> CreateSourceFileAsync()
    {
        var sourcePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.csv");
        await File.WriteAllTextAsync(sourcePath, "content");
        return sourcePath;
    }

    private static async Task<RunResult> RunAsync(
        string[] arguments,
        Func<UploaderOptions> optionsLoader,
        Func<UploaderOptions, IBlobUploader>? uploaderFactory = null)
    {
        using var standardOutput = new StringWriter();
        using var standardError = new StringWriter();

        var exitCode = await UploaderApplication.RunAsync(
            arguments,
            optionsLoader,
            standardOutput,
            standardError,
            uploaderFactory ?? (_ => new FakeBlobUploader()),
            CancellationToken.None);

        return new RunResult(exitCode, standardOutput.ToString(), standardError.ToString());
    }

    private static UploaderOptions ValidOptions() => new()
    {
        ConnectionString = SensitiveValue,
        DefaultContainer = "default-container"
    };

    private sealed record RunResult(
        int ExitCode,
        string StandardOutput,
        string StandardError);
}