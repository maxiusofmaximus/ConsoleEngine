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

    // ── Instance properties (persisted) ───────────────────────────────────────

    public string Language           { get; set; } = EnglishLanguage;

    // Audio
    public int  MasterVolume         { get; set; } = 100;
    public int  MusicVolume          { get; set; } = 80;
    public int  SfxVolume            { get; set; } = 90;
    public int  AmbienceVolume       { get; set; } = 70;
    public int  UiVolume             { get; set; } = 85;
    public bool MuteAudioOnFocusLoss { get; set; } = true;

    // Display
    public string DisplayMode        { get; set; } = "borderless";
    public string AspectRatio        { get; set; } = "16:9";
    public int    ResolutionWidth    { get; set; } = 1600;
    public int    ResolutionHeight   { get; set; } = 900;

    // Optional AI provider (used by games that integrate an AI assistant)
    public string AiProvider         { get; set; } = "disabled";
    public string BaseUrl            { get; set; } = string.Empty;
    public string ApiKey             { get; set; } = string.Empty;
    public string Model              { get; set; } = string.Empty;
}
