using ConsoleEngine.Core;
using ConsoleEngine.Locale;
using Xunit;

namespace ConsoleEngine.Tests;

public sealed class LocalizationServiceTests
{
    private static InMemoryLocalizationService MakeService(
        string lang = "en",
        Dictionary<string, string>? extra = null)
    {
        var enEntries = new Dictionary<string, string>
        {
            ["greeting"]  = "Hello",
            ["farewell"]  = "Goodbye",
            ["formatted"] = "Hello, {0}!",
        };
        if (extra is not null)
            foreach (var (k, v) in extra)
                enEntries[k] = v;

        var table = new LocalizationTable("en", enEntries);
        return new InMemoryLocalizationService(table);
    }

    [Fact]
    public void Resolve_ExistingKey_ReturnsTranslation()
    {
        var svc = MakeService();
        Assert.Equal("Hello", svc.Resolve("greeting"));
    }

    [Fact]
    public void Resolve_MissingKey_ReturnsBracketedKey()
    {
        var svc = MakeService();
        Assert.Equal("[missing.key]", svc.Resolve("missing.key"));
    }

    [Fact]
    public void Resolve_WithArgs_FormatsString()
    {
        var svc = MakeService();
        Assert.Equal("Hello, Ari!", svc.Resolve("formatted", "Ari"));
    }

    [Fact]
    public void SetLanguage_UnknownLanguage_FallsBackToEnglish()
    {
        var svc = MakeService();
        svc.SetLanguage("xx");
        Assert.Equal("en", svc.CurrentLanguage);
    }

    [Fact]
    public void LanguageChanged_FiresOnActualChange()
    {
        var svc = MakeService();
        var esTable = new LocalizationTable("es", new Dictionary<string, string>
        {
            ["greeting"] = "Hola",
        });
        svc.LoadTable(esTable);

        string? receivedLang = null;
        svc.LanguageChanged += lang => receivedLang = lang;

        svc.SetLanguage("es");

        Assert.Equal("es", receivedLang);
        Assert.Equal("Hola", svc.Resolve("greeting"));
    }

    [Fact]
    public void LanguageChanged_DoesNotFireIfLanguageUnchanged()
    {
        var svc = MakeService();
        int fired = 0;
        svc.LanguageChanged += _ => fired++;

        svc.SetLanguage("en"); // already "en"

        Assert.Equal(0, fired);
    }

    [Fact]
    public void FallsBackToEnglish_WhenKeyMissingInCurrentLanguage()
    {
        var svc = MakeService();
        var esTable = new LocalizationTable("es", new Dictionary<string, string>
        {
            ["greeting"] = "Hola",
            // "farewell" intentionally absent — should fall back to EN
        });
        svc.LoadTable(esTable);
        svc.SetLanguage("es");

        Assert.Equal("Hola",    svc.Resolve("greeting")); // es translation
        Assert.Equal("Goodbye", svc.Resolve("farewell")); // en fallback
    }

    [Fact]
    public void CL_Get_ReturnsKeyWhenNotInitialised()
    {
        string result = CL.Get("some.key.no.locale");
        Assert.Equal("some.key.no.locale", result);
    }

    [Fact]
    public void Resolve_WithMultipleArgs_FormatsAllPlaceholders()
    {
        var svc = MakeService(extra: new Dictionary<string, string>
        {
            ["multi"] = "{0} and {1}",
        });
        Assert.Equal("Alpha and Beta", svc.Resolve("multi", "Alpha", "Beta"));
    }

    [Fact]
    public void AvailableLanguages_ContainsLoadedLanguages()
    {
        var svc = MakeService();
        var esTable = new LocalizationTable("es", new Dictionary<string, string>
        {
            ["greeting"] = "Hola",
        });
        svc.LoadTable(esTable);

        Assert.Contains("en", svc.AvailableLanguages);
        Assert.Contains("es", svc.AvailableLanguages);
    }

    [Fact]
    public void SetLanguage_ValidLanguage_ChangesCurrentLanguage()
    {
        var svc = MakeService();
        svc.LoadTable(new LocalizationTable("fr", new Dictionary<string, string>
        {
            ["greeting"] = "Bonjour",
        }));
        svc.SetLanguage("fr");
        Assert.Equal("fr", svc.CurrentLanguage);
        Assert.Equal("Bonjour", svc.Resolve("greeting"));
    }

    [Fact]
    public void LoadTable_OverridesExistingTable_WhenSameLanguage()
    {
        var svc = MakeService();
        svc.LoadTable(new LocalizationTable("en", new Dictionary<string, string>
        {
            ["greeting"] = "Hey",
        }));
        Assert.Equal("Hey", svc.Resolve("greeting"));
    }

    [Fact]
    public void Resolve_MissingKeyInBothLanguages_ReturnsBracketedKey()
    {
        var svc = MakeService();
        svc.LoadTable(new LocalizationTable("es", new Dictionary<string, string>()));
        svc.SetLanguage("es");

        string result = svc.Resolve("totally.missing");
        Assert.Equal("[totally.missing]", result);
    }
}
