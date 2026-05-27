using System;
using System.Collections.Generic;

namespace ConsoleEngine.Config;

/// <summary>
/// Shared game configuration consumed by both the CLI runtime and the Unity/Godot front-end.
/// Serialised to <c>Config/config.json</c> by <c>GameConfigRepository</c>.
/// </summary>
[Serializable]
public class GameConfig
{
    // ── Language ──────────────────────────────────────────────────────────────

    public const string EnglishLanguage = "en";

    public static readonly string[] SupportedLanguages =
    {
        "en", "ja", "zh-Hans", "es", "pt", "de", "ru", "it", "fr", "ca", "ko"
    };

    public static readonly Dictionary<string, string> LanguageLabels =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"]      = "English",
            ["ja"]      = "Japanese",
            ["zh-Hans"] = "Simplified Chinese",
            ["es"]      = "Spanish",
            ["pt"]      = "Portuguese",
            ["de"]      = "German",
            ["ru"]      = "Russian",
            ["it"]      = "Italian",
            ["fr"]      = "French",
            ["ca"]      = "Catalan",
            ["ko"]      = "Korean"
        };

    // ── Display ───────────────────────────────────────────────────────────────

    public static readonly string[] SupportedAspectRatios   = { "4:3", "16:9", "16:10" };
    public static readonly string[] SupportedDisplayModes   = { "windowed", "borderless", "fullscreen" };

    // ── Instance fields (persisted) ───────────────────────────────────────────

    public string Language         = EnglishLanguage;

    // Audio
    public int  MasterVolume       = 100;
    public int  MusicVolume        = 80;
    public int  SfxVolume          = 90;
    public int  AmbienceVolume     = 70;
    public int  UiVolume           = 85;
    public bool MuteAudioOnFocusLoss = true;

    // Display
    public string DisplayMode      = "borderless";
    public string AspectRatio      = "16:9";
    public int    ResolutionWidth  = 1600;
    public int    ResolutionHeight = 900;

    // Optional AI provider (used by games that integrate an AI assistant)
    public string AiProvider       = "disabled";
    public string BaseUrl          = string.Empty;
    public string ApiKey           = string.Empty;
    public string Model            = string.Empty;
}
