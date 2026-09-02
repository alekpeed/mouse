using System.Text.Json;
using System.Text.Json.Serialization;

namespace PeripheralCompanion;

/// <summary>
/// Portable settings persisted next to the executable so that configuration
/// travels with the USB stick rather than being written to the host machine's
/// user profile.
/// </summary>
public sealed class Settings
{
    public JiggleMode Mode { get; set; } = JiggleMode.Invisible;
    public int IntervalSeconds { get; set; } = 60;
    public bool RespectUserActivity { get; set; } = true;
    public bool KeepDisplayAwake { get; set; } = true;

    /// <summary>Start jiggling automatically when the app launches.</summary>
    public bool AutoStart { get; set; } = true;

    [JsonIgnore]
    private static string FilePath =>
        Path.Combine(AppContext.BaseDirectory, "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public static Settings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                string json = File.ReadAllText(FilePath);
                return JsonSerializer.Deserialize<Settings>(json, JsonOptions) ?? new Settings();
            }
        }
        catch
        {
            // A read-only USB stick or malformed file falls back to defaults.
        }
        return new Settings();
    }

    public void Save()
    {
        try
        {
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, JsonOptions));
        }
        catch
        {
            // Persistence is best-effort; the app still runs from defaults.
        }
    }
}
