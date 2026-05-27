using System;
using System.Linq;

namespace ConsoleEngine.Config;

/// <summary>
/// Applies CLI settings commands (<c>set language en</c>, <c>set master 80</c>, etc.)
/// to a <see cref="GameConfig"/> instance and produces human-readable feedback messages.
/// </summary>
public static class GameSettingsCommands
{
    /// <summary>
    /// Applies a setting by name and value. Returns <c>true</c> on success.
    /// <paramref name="message"/> always contains a feedback string.
    /// </summary>
    public static bool TryApply(GameConfig config, string settingName, string value, out string message)
    {
        ArgumentNullException.ThrowIfNull(config);
        if (string.IsNullOrWhiteSpace(settingName)) { message = "Unknown setting."; return false; }

        string key     = settingName.Trim().ToLowerInvariant();
        string trimmed = value?.Trim() ?? string.Empty;

        switch (key)
        {
            case "language":
                config.Language = GameSettingsCatalog.NormalizeLanguage(trimmed);
                message = $"Language set to {GameConfig.LanguageLabels[config.Language]} ({config.Language}).";
                return true;

            case "master":
                config.MasterVolume = ParseVolume(trimmed);
                message = $"Master volume set to {config.MasterVolume}.";
                return true;

            case "music":
                config.MusicVolume = ParseVolume(trimmed);
                message = $"Music volume set to {config.MusicVolume}.";
                return true;

            case "sfx":
                config.SfxVolume = ParseVolume(trimmed);
                message = $"SFX volume set to {config.SfxVolume}.";
                return true;

            case "ambience":
                config.AmbienceVolume = ParseVolume(trimmed);
                message = $"Ambience volume set to {config.AmbienceVolume}.";
                return true;

            case "ui":
                config.UiVolume = ParseVolume(trimmed);
                message = $"UI volume set to {config.UiVolume}.";
                return true;

            case "backgroundmute":
                config.MuteAudioOnFocusLoss = ParseBoolean(trimmed);
                message = $"Mute on focus loss: {config.MuteAudioOnFocusLoss}.";
                return true;

            case "display":
                config.DisplayMode = GameSettingsCatalog.NormalizeDisplayMode(trimmed);
                message = $"Display mode set to {config.DisplayMode}.";
                return true;

            case "aspect":
                config.AspectRatio = GameSettingsCatalog.NormalizeAspectRatio(trimmed);
                var fallback = GameSettingsCatalog.GetResolutionPresets(config.AspectRatio)[0];
                config.ResolutionWidth  = fallback.Width;
                config.ResolutionHeight = fallback.Height;
                message = $"Aspect ratio set to {config.AspectRatio} with resolution {fallback.Label}.";
                return true;

            case "resolution":
                foreach (string ar in GameConfig.SupportedAspectRatios)
                    if (GameSettingsCatalog.TryApplyResolutionPreset(config, ar, trimmed))
                    {
                        message = $"Resolution set to {trimmed} ({config.AspectRatio}).";
                        return true;
                    }
                message = $"Unknown resolution preset '{trimmed}'.";
                return false;

            default:
                message = "Unknown setting.";
                return false;
        }
    }

    /// <summary>Returns a multi-line summary of the current config.</summary>
    public static string Describe(GameConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        string langLabel  = GameConfig.LanguageLabels.TryGetValue(config.Language, out string? lbl) ? lbl : config.Language;
        string resolution = $"{config.ResolutionWidth}x{config.ResolutionHeight} ({config.AspectRatio}, {config.DisplayMode})";
        return string.Join(Environment.NewLine, new[]
        {
            $"language:   {langLabel} ({config.Language})",
            $"audio:      master {config.MasterVolume}  music {config.MusicVolume}  sfx {config.SfxVolume}  ambience {config.AmbienceVolume}  ui {config.UiVolume}",
            $"focus mute: {config.MuteAudioOnFocusLoss}",
            $"display:    {resolution}"
        });
    }

    /// <summary>Returns the list of supported languages with usage hint.</summary>
    public static string DescribeSupportedLanguages()
    {
        var lines = GameConfig.SupportedLanguages
            .Select(code => $"  - {code}: {GameConfig.LanguageLabels[code]}")
            .ToList();
        lines.Add("");
        lines.Add("  To switch:  type the code or name directly  (e.g.  en  or  English)");
        return string.Join(Environment.NewLine, lines);
    }

    /// <summary>Returns the list of resolution presets for the given aspect ratio with usage hint.</summary>
    public static string DescribeResolutionPresets(string aspectRatio)
    {
        var lines = GameSettingsCatalog.GetResolutionPresets(aspectRatio)
            .Select(p => $"  - {p.Label}")
            .ToList();
        lines.Add("");
        lines.Add("  To switch:  set resolution <preset>  (e.g.  set resolution 1920x1080)");
        return string.Join(Environment.NewLine, lines);
    }

    // ── Parsing helpers ───────────────────────────────────────────────────────

    private static int ParseVolume(string value)
    {
        if (!int.TryParse(value, out int parsed))
            throw new ArgumentException("Volume must be a whole number between 0 and 100.", nameof(value));
        return GameSettingsCatalog.ClampVolume(parsed);
    }

    private static bool ParseBoolean(string value)
    {
        if (bool.TryParse(value, out bool parsed)) return parsed;
        return value.ToLowerInvariant() switch
        {
            "on"  or "yes" or "1" => true,
            "off" or "no"  or "0" => false,
            _ => throw new ArgumentException("Boolean value must be true/false, on/off, yes/no, or 1/0.", nameof(value))
        };
    }

    internal static string Mask(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        if (value.Length <= 4) return "****";
        return new string('*', value.Length - 4) + value[^4..];
    }
}
