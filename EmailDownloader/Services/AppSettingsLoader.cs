using EmailDownloader.Configuration;
using System.Text.Json;

namespace EmailDownloader.Services;

public static class AppSettingsLoader
{
    public static AppSettings LoadSettings(string settingsFileName = "appsettings.json")
    {
        var probePaths = new[]
        {
            Path.Combine(Environment.CurrentDirectory, settingsFileName),
            Path.Combine(AppContext.BaseDirectory, settingsFileName)
        };

        var foundPath = probePaths.FirstOrDefault(File.Exists);
        if (foundPath == null)
        {
            throw new FileNotFoundException(
                $"Configuration file '{settingsFileName}' not found. " +
                $"Please copy 'appsettings.example.json' to '{settingsFileName}' and update the values. " +
                $"(Searched: {string.Join(", ", probePaths)})");
        }

        try
        {
            var json = File.ReadAllText(foundPath);
            return DeserializeWithSanitization(json, foundPath);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"Invalid JSON in '{foundPath}': {ex.Message}", ex);
        }
    }

    private static AppSettings DeserializeWithSanitization(string json, string sourcePath)
    {
        try
        {
            var settings = JsonSerializer.Deserialize<AppSettings>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (settings == null)
            {
                throw new InvalidOperationException($"Failed to deserialize {sourcePath}");
            }

            return settings;
        }
        catch (JsonException)
        {
            var trimmed = json.TrimStart();
            while (trimmed.StartsWith("{{"))
            {
                trimmed = trimmed.Substring(1);
            }

            var settings = JsonSerializer.Deserialize<AppSettings>(trimmed, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (settings == null)
            {
                throw new InvalidOperationException($"Failed to deserialize {sourcePath} after sanitization");
            }

            return settings;
        }
    }
}
