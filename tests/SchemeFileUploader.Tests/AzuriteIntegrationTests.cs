using Azure.Storage.Blobs;

namespace SchemeFileUploader.Tests;

public sealed class AzuriteIntegrationTests
{
    private const string ConnectionString = "UseDevelopmentStorage=true";

    private static UploaderOptions CreateOptions(string containerName) =>
        new()
        {
            ConnectionString = ConnectionString,
            ContainerName = containerName
        };

    private static string CreateContainerName() => $"sfu-it-{Guid.NewGuid():N}";

    private static BlobServiceClient CreateServiceClient() =>
        new(
            ConnectionString,
            new BlobClientOptions(BlobClientOptions.ServiceVersion.V2025_11_05));

    private static string CreateTestDirectory()
    {
        var directoryPath = Path.Combine(
            Path.GetTempPath(),
            $"scheme-file-uploader-integration-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directoryPath);
        return directoryPath;
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GivenConcurrentSameNameCreates_WhenUploading_ExpectedExactlyOneSuccess()
    {
        var containerName = CreateContainerName();
        var firstDirectory = CreateTestDirectory();
        var secondDirectory = CreateTestDirectory();
        var serviceClient = CreateServiceClient();

        try
        {
            var firstPath = Path.Combine(firstDirectory, "race.csv");
            var secondPath = Path.Combine(secondDirectory, "race.csv");
            await File.WriteAllBytesAsync(firstPath, [1, 2, 3]);
            await File.WriteAllBytesAsync(secondPath, [4, 5, 6]);
            var uploader = new AzureBlobUploader(serviceClient);
            var firstService = new UploadService(uploader, CreateOptions(containerName));
            var secondService = new UploadService(uploader, CreateOptions(containerName));

            var results = await Task.WhenAll(
                firstService.UploadAsync(firstPath),
                secondService.UploadAsync(secondPath));

            Assert.Single(results, result => result.Outcome == UploadOutcome.Uploaded);
            Assert.Single(results, result => result.Outcome == UploadOutcome.Refused);
            var blobClient = serviceClient
                .GetBlobContainerClient(containerName)
                .GetBlobClient("race.csv");
            var storedBytes = (await blobClient.DownloadContentAsync()).Value.Content.ToArray();
            byte[] firstExpectedBytes = [1, 2, 3];
            byte[] secondExpectedBytes = [4, 5, 6];
            Assert.True(
                storedBytes.SequenceEqual(firstExpectedBytes)
                    || storedBytes.SequenceEqual(secondExpectedBytes));
        }
        finally
        {
            await serviceClient.GetBlobContainerClient(containerName).DeleteIfExistsAsync();
            Directory.Delete(firstDirectory, recursive: true);
            Directory.Delete(secondDirectory, recursive: true);
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GivenKnownBytes_WhenUploadingThroughRealSdk_ExpectedExactRoundTrip()
    {
        var containerName = CreateContainerName();
        var directoryPath = CreateTestDirectory();
        var serviceClient = CreateServiceClient();

        try
        {
            byte[] expectedBytes = [0, 1, 2, 10, 13, 254, 255];
            var sourcePath = Path.Combine(directoryPath, "known-bytes.csv");
            await File.WriteAllBytesAsync(sourcePath, expectedBytes);
            var service = new UploadService(
                new AzureBlobUploader(serviceClient),
                CreateOptions(containerName));

            var result = await service.UploadAsync(sourcePath);

            var blobClient = serviceClient
                .GetBlobContainerClient(containerName)
                .GetBlobClient("known-bytes.csv");
            var download = await blobClient.DownloadContentAsync();
            var properties = await blobClient.GetPropertiesAsync();
            Assert.Equal(UploadOutcome.Uploaded, result.Outcome);
            Assert.Equal("known-bytes.csv", result.BlobName);
            Assert.Equal(containerName, result.ContainerName);
            Assert.Equal(expectedBytes.Length, result.BytesWritten);
            Assert.Equal(expectedBytes, download.Value.Content.ToArray());
            Assert.Equal(expectedBytes.Length, properties.Value.ContentLength);
        }
        finally
        {
            await serviceClient.GetBlobContainerClient(containerName).DeleteIfExistsAsync();
            Directory.Delete(directoryPath, recursive: true);
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GivenRepeatedBlobName_WhenUploading_ExpectedSecondUploadRefused()
    {
        var containerName = CreateContainerName();
        var directoryPath = CreateTestDirectory();
        var serviceClient = CreateServiceClient();

        try
        {
            var sourcePath = Path.Combine(directoryPath, "repeat.csv");
            await File.WriteAllBytesAsync(sourcePath, [7, 8, 9]);
            var service = new UploadService(
                new AzureBlobUploader(serviceClient),
                CreateOptions(containerName));

            var firstResult = await service.UploadAsync(sourcePath);
            var secondResult = await service.UploadAsync(sourcePath);

            Assert.Equal(UploadOutcome.Uploaded, firstResult.Outcome);
            Assert.Equal(UploadOutcome.Refused, secondResult.Outcome);
            Assert.Equal(0, secondResult.BytesWritten);
        }
        finally
        {
            await serviceClient.GetBlobContainerClient(containerName).DeleteIfExistsAsync();
            Directory.Delete(directoryPath, recursive: true);
        }
    }
}