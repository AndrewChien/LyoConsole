using System.IO;
using System.Text.Json;
using LyoConsole.Models;

namespace LyoConsole.Services;

public sealed class SettingsStorage
{
    private const string SettingsFileName = "LyoConsole.settings.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    public string PrimaryPath { get; } = Path.Combine(AppContext.BaseDirectory, SettingsFileName);

    public string FallbackPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "LyoConsole",
        SettingsFileName);

    public async Task<(AppSettings Settings, string LoadedFromPath)> LoadAsync()
    {
        var (settings, loadedFromPath) = await TryLoadFromAsync(PrimaryPath).ConfigureAwait(false);
        if (settings is not null)
            return (settings, loadedFromPath);

        (settings, loadedFromPath) = await TryLoadFromAsync(FallbackPath).ConfigureAwait(false);
        if (settings is not null)
            return (settings, loadedFromPath);

        settings = CreateDefault();
        loadedFromPath = await TrySaveToAsync(settings, PrimaryPath).ConfigureAwait(false)
            ? PrimaryPath
            : await TrySaveToAsync(settings, FallbackPath).ConfigureAwait(false)
                ? FallbackPath
                : PrimaryPath;

        return (settings, loadedFromPath);
    }

    public async Task SaveAsync(AppSettings settings)
    {
        if (await TrySaveToAsync(settings, PrimaryPath).ConfigureAwait(false))
            return;

        await TrySaveToAsync(settings, FallbackPath).ConfigureAwait(false);
    }

    private static AppSettings CreateDefault()
    {
        return new AppSettings
        {
            Programs = [],
            Ui = new UiSettings(),
        };
    }

    private static async Task<(AppSettings? Settings, string LoadedFromPath)> TryLoadFromAsync(string path)
    {
        try
        {
            if (!File.Exists(path))
                return (null, path);

            var json = await File.ReadAllTextAsync(path).ConfigureAwait(false);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
            return (settings ?? CreateDefault(), path);
        }
        catch
        {
            return (null, path);
        }
    }

    private static async Task<bool> TrySaveToAsync(AppSettings settings, string path)
    {
        try
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(dir))
                Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(settings, JsonOptions);
            await File.WriteAllTextAsync(path, json).ConfigureAwait(false);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

