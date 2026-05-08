using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using CCWaterControllerPlayer.Models;

namespace CCWaterControllerPlayer.Services;

public class SettingsService
{
    private readonly string _settingsPath;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
    };

    public AppSettings Settings { get; set; } = new();

    public SettingsService(string? settingsPath = null)
    {
        _settingsPath = settingsPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CCWaterControllerPlayer", "settings.json");
    }

    public async Task LoadAsync()
    {
        if (File.Exists(_settingsPath))
        {
            try
            {
                var json = await File.ReadAllTextAsync(_settingsPath);
                Settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
            }
            catch
            {
                Settings = new AppSettings();
            }
        }
    }

    public async Task SaveAsync()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);
        var json = JsonSerializer.Serialize(Settings, JsonOptions);
        await File.WriteAllTextAsync(_settingsPath, json);
    }
}
