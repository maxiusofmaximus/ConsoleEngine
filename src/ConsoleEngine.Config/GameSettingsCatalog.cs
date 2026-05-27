using System;
using System.Collections.Generic;
using System.Linq;

namespace ConsoleEngine.Config;

/// <summary>
/// Resolution presets, volume clamping, and value normalisation helpers.
/// Used by <see cref="GameSettingsCommands"/> to validate user input.
/// </summary>
public static class GameSettingsCatalog
{
    // ── Resolution presets ────────────────────────────────────────────────────

    private static readonly Dictionary<string, ResolutionPreset[]> ResolutionPresets =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["4:3"]   = new[] { new ResolutionPreset(1024, 768),  new ResolutionPreset(1280, 960),  new ResolutionPreset(1440, 1080), new ResolutionPreset(1600, 1200) },
            ["16:9"]  = new[] { new ResolutionPreset(1280, 720),  new ResolutionPreset(1600, 900),  new ResolutionPreset(1920, 1080), new ResolutionPreset(2560, 1440) },
            ["16:10"] = new[] { new ResolutionPreset(1280, 800),  new ResolutionPreset(1440, 900),  new ResolutionPreset(1680, 1050), new ResolutionPreset(1920, 1200) }
        };

    public static IReadOnlyList<ResolutionPreset> GetResolutionPresets(string aspectRatio)
    {
        if (!ResolutionPresets.TryGetValue(NormalizeAspectRatio(aspectRatio), out var presets))
            presets = ResolutionPresets["16:9"];
        return presets;
    }

    public static bool TryApplyResolutionPreset(GameConfig config, string aspectRatio, string label)
    {
        foreach (var preset in GetResolutionPresets(aspectRatio))
        {
            if (!string.Equals(preset.Label, label, StringComparison.OrdinalIgnoreCase)) continue;
            config.AspectRatio      = NormalizeAspectRatio(aspectRatio);
            config.ResolutionWidth  = preset.Width;
            config.ResolutionHeight = preset.Height;
            return true;
        }
        return false;
    }

    // ── Normalisation ─────────────────────────────────────────────────────────

    public static string NormalizeLanguage(string language)
    {
        string? normalized = language?.Trim();
        string? supported  = GameConfig.SupportedLanguages.FirstOrDefault(c =>
            c.Equals(normalized, StringComparison.OrdinalIgnoreCase));
        return string.IsNullOrEmpty(supported) ? GameConfig.EnglishLanguage : supported;
    }

    public static string NormalizeAspectRatio(string aspectRatio)
    {
        string? normalized = aspectRatio?.Trim();
        string? supported  = GameConfig.SupportedAspectRatios.FirstOrDefault(c =>
            c.Equals(normalized, StringComparison.OrdinalIgnoreCase));
        return string.IsNullOrEmpty(supported) ? "16:9" : supported;
    }

    public static string NormalizeDisplayMode(string displayMode)
    {
        string? normalized = displayMode?.Trim().ToLowerInvariant();
        string? supported  = GameConfig.SupportedDisplayModes.FirstOrDefault(c =>
            c.Equals(normalized, StringComparison.OrdinalIgnoreCase));
        return string.IsNullOrEmpty(supported) ? "borderless" : supported;
    }

    public static int ClampVolume(int value) => Math.Clamp(value, 0, 100);

    /// <summary>
    /// Resolves a bare language code ("en") or name ("English") to the canonical code.
    /// Returns <c>null</c> when the input is not recognised.
    /// </summary>
    public static string? TryResolveLanguageShorthand(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;
        string trimmed = input.Trim();

        string? byCode = GameConfig.SupportedLanguages.FirstOrDefault(c =>
            c.Equals(trimmed, StringComparison.OrdinalIgnoreCase));
        if (byCode != null) return byCode;

        foreach (KeyValuePair<string, string> pair in GameConfig.LanguageLabels)
            if (pair.Value.Equals(trimmed, StringComparison.OrdinalIgnoreCase))
                return pair.Key;

        return null;
    }

    // ── Normalise on load ─────────────────────────────────────────────────────

    /// <summary>Normalises all persisted values on load to prevent corrupt-config crashes.</summary>
    public static void NormalizeOnLoad(GameConfig config)
    {
        config.Language      = NormalizeLanguage(config.Language);
        config.DisplayMode   = NormalizeDisplayMode(config.DisplayMode);
        config.AspectRatio   = NormalizeAspectRatio(config.AspectRatio);
        config.MasterVolume  = ClampVolume(config.MasterVolume);
        config.MusicVolume   = ClampVolume(config.MusicVolume);
        config.SfxVolume     = ClampVolume(config.SfxVolume);
        config.AmbienceVolume= ClampVolume(config.AmbienceVolume);
        config.UiVolume      = ClampVolume(config.UiVolume);
    }
}
