using Microsoft.Extensions.Configuration;
using SchemeFileUploader;
using SchemeFileUploader.Cli;

internal static class Program
{
	public static async Task<int> Main(string[] args)
	{
		Func<UploaderOptions> optionsLoader = () => LoadOptions(
			Path.Combine(AppContext.BaseDirectory, "appsettings.json"));
		Func<UploaderOptions, IBlobUploader> uploaderFactory = options =>
			new AzureBlobUploader(options);

		return await UploaderApplication.RunAsync(
			args,
			optionsLoader,
			Console.Out,
			Console.Error,
			uploaderFactory,
			CancellationToken.None);
	}

	internal static UploaderOptions LoadOptions(string configurationPath)
	{
		IConfigurationRoot configuration;

		try
		{
			configuration = new ConfigurationBuilder()
				.AddJsonFile(configurationPath, optional: false, reloadOnChange: false)
				.Build();
		}
		catch (FileNotFoundException)
		{
			throw new UploaderConfigurationException(
				"Configuration file 'appsettings.json' was not found.");
		}
		catch (InvalidDataException)
		{
			throw new UploaderConfigurationException(
				"Configuration file 'appsettings.json' contains malformed JSON.");
		}
		catch (FormatException)
		{
			throw new UploaderConfigurationException(
				"Configuration file 'appsettings.json' contains malformed JSON.");
		}

		var section = configuration.GetSection("Uploader");
		ValidateRequiredSetting(section, nameof(UploaderOptions.ConnectionString));
		ValidateRequiredSetting(section, nameof(UploaderOptions.DefaultContainer));

		return section.Get<UploaderOptions>()
			?? throw new UploaderConfigurationException(
				"Configuration section 'Uploader' is required.");
	}

	private static void ValidateRequiredSetting(
		IConfigurationSection section,
		string settingName)
	{
		if (string.IsNullOrWhiteSpace(section[settingName]))
		{
			throw new UploaderConfigurationException(
				$"Configuration setting 'Uploader:{settingName}' is required.");
		}
	}
}
