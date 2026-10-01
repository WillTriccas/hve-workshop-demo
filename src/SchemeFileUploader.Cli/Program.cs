using System.Globalization;
using Microsoft.Extensions.Configuration;

namespace SchemeFileUploader.Cli;

internal static class Program
{
    private const int UploadedExitCode = 0;
    private const int RefusedExitCode = 1;
    private const int FailedExitCode = 2;
    private const string UsageMessage =
        "Usage: SchemeFileUploader.Cli <local-file> [--container <name>]";
    private const string CompositionFailureMessage =
        "Upload failed. Check appsettings.json, Uploader__* environment variables, and the storage emulator.";

    internal static async Task<int> Main(string[] args)
    {
        if (!TryParseArguments(args, out var sourcePath, out var containerOverride))
        {
            Console.Error.WriteLine(UsageMessage);
            return FailedExitCode;
        }

        try
        {
            var options = LoadOptions(containerOverride);
            options.Validate();

            var blobUploader = new AzureBlobUploader(options.ConnectionString);
            var uploadService = new UploadService(blobUploader, options);
            var result = await uploadService.UploadAsync(sourcePath).ConfigureAwait(false);

            return WriteResult(result);
        }
        catch (Exception)
        {
            Console.Error.WriteLine(CompositionFailureMessage);
            return FailedExitCode;
        }
    }

    internal static UploaderOptions LoadOptions(string? containerOverride)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();

        var options = new UploaderOptions();
        configuration.GetSection("Uploader").Bind(options);

        if (containerOverride is not null)
        {
            options.ContainerName = containerOverride;
        }

        return options;
    }

    internal static bool TryParseArguments(
        string[] args,
        out string sourcePath,
        out string? containerOverride)
    {
        sourcePath = string.Empty;
        containerOverride = null;

        for (var index = 0; index < args.Length; index++)
        {
            var argument = args[index];
            if (string.Equals(argument, "--container", StringComparison.Ordinal))
            {
                if (containerOverride is not null
                    || index + 1 >= args.Length
                    || string.IsNullOrWhiteSpace(args[index + 1])
                    || args[index + 1].StartsWith("--", StringComparison.Ordinal))
                {
                    return false;
                }

                containerOverride = args[++index];
                continue;
            }

            if (argument.StartsWith("--", StringComparison.Ordinal)
                || string.IsNullOrWhiteSpace(argument)
                || sourcePath.Length > 0)
            {
                return false;
            }

            sourcePath = argument;
        }

        return sourcePath.Length > 0;
    }

    internal static int WriteResult(UploadResult result)
    {
        switch (result.Outcome)
        {
            case UploadOutcome.Uploaded:
                var blobName = SanitizeForOutput(result.BlobName);
                Console.WriteLine(
                    $"Uploaded blob '{blobName}' to container '{result.ContainerName}' "
                    + $"({result.BytesWritten.ToString(CultureInfo.InvariantCulture)} bytes).");
                return UploadedExitCode;

            case UploadOutcome.Refused:
                Console.Error.WriteLine(
                    "Upload refused. A blob with the same name already exists; rename the file or remove the existing blob.");
                return RefusedExitCode;

            case UploadOutcome.Failed:
            default:
                Console.Error.WriteLine(
                    $"Upload failed. {result.Message} Check the source file and storage emulator, then retry.");
                return FailedExitCode;
        }
    }

    private static string SanitizeForOutput(string value) =>
        string.Concat(value.Select(character => char.IsControl(character) ? '?' : character));
}