using System.Text.Json;

namespace EmailDownloader.Desktop.Configuration;

public class DesktopSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public string SettingsPath { get; }

    public DesktopSettingsStore(string? settingsPath = null)
    {
        SettingsPath = settingsPath ?? Path.Combine(AppContext.BaseDirectory, "desktopsettings.json");
    }

    public DesktopAppSettings Load()
    {
        if (!File.Exists(SettingsPath))
        {
            return new DesktopAppSettings();
        }

        try
        {
            var json = File.ReadAllText(SettingsPath);
            var settings = JsonSerializer.Deserialize<DesktopAppSettings>(json, JsonOptions);
            return settings ?? new DesktopAppSettings();
        }
        catch
        {
            return new DesktopAppSettings();
        }
    }

    public void Save(DesktopAppSettings settings)
    {
        var dir = Path.GetDirectoryName(SettingsPath);
        if (!string.IsNullOrWhiteSpace(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(SettingsPath, json);
    }
}
