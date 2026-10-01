using SchemeFileUploader;

namespace SchemeFileUploader.Cli;

internal static class UploaderApplication
{
    private const string Usage = "Usage: uploader <file> [--container <name>]";

    internal static async Task<int> RunAsync(
        string[] args,
        Func<UploaderOptions> optionsLoader,
        TextWriter standardOutput,
        TextWriter standardError,
        Func<UploaderOptions, IBlobUploader> uploaderFactory,
        CancellationToken cancellationToken)
    {
        try
        {
            var parseResult = ParseArguments(args);
            if (!parseResult.IsValid)
            {
                await standardError.WriteLineAsync(parseResult.ErrorMessage);
                await standardError.WriteLineAsync(Usage);
                return 2;
            }

            var options = optionsLoader();
            var container = parseResult.Container ?? options.DefaultContainer;
            var uploader = uploaderFactory(options);
            var service = new UploadService(uploader);
            var result = await service.UploadAsync(
                parseResult.SourcePath,
                container,
                cancellationToken);

            if (result.Outcome == UploadOutcome.Uploaded)
            {
                await standardOutput.WriteLineAsync(result.Message);
                return 0;
            }

            await standardError.WriteLineAsync(result.Message);
            return 2;
        }
        catch (UploaderConfigurationException exception)
        {
            await standardError.WriteLineAsync($"Configuration error: {exception.Message}");
            return 2;
        }
        catch (OperationCanceledException)
        {
            await standardError.WriteLineAsync("The upload was canceled.");
            return 2;
        }
        catch (Exception)
        {
            await standardError.WriteLineAsync(
                "The upload failed unexpectedly. Verify the configuration and storage availability, then try again.");
            return 2;
        }
    }

    private static ParseResult ParseArguments(string[] args)
    {
        if (args.Length == 0 || string.IsNullOrWhiteSpace(args[0]))
        {
            return ParseResult.Failure("A source file path is required.");
        }

        if (args[0].StartsWith("--", StringComparison.Ordinal))
        {
            return ParseResult.Failure($"Unknown option '{args[0]}'.");
        }

        if (args.Length == 1)
        {
            return ParseResult.Success(args[0], null);
        }

        if (!string.Equals(args[1], "--container", StringComparison.Ordinal))
        {
            var message = args[1].StartsWith("--", StringComparison.Ordinal)
                ? $"Unknown option '{args[1]}'."
                : "Only one source file path can be provided.";
            return ParseResult.Failure(message);
        }

        if (args.Length == 2 || string.IsNullOrWhiteSpace(args[2]))
        {
            return ParseResult.Failure("Option '--container' requires a container name.");
        }

        if (args[2].StartsWith("--", StringComparison.Ordinal))
        {
            return ParseResult.Failure("Option '--container' requires a container name.");
        }

        if (args.Length > 3)
        {
            var message = args.Skip(3).Any(
                argument => string.Equals(argument, "--container", StringComparison.Ordinal))
                ? "Option '--container' can be specified only once."
                : "Unexpected extra arguments were provided.";
            return ParseResult.Failure(message);
        }

        return ParseResult.Success(args[0], args[2]);
    }

    private sealed record ParseResult(
        bool IsValid,
        string SourcePath,
        string? Container,
        string ErrorMessage)
    {
        internal static ParseResult Failure(string errorMessage) =>
            new(false, string.Empty, null, errorMessage);

        internal static ParseResult Success(string sourcePath, string? container) =>
            new(true, sourcePath, container, string.Empty);
    }
}

internal sealed class UploaderConfigurationException(string message) : Exception(message);