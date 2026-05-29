using ConsoleEngine.Scenes;
using Xunit;

namespace ConsoleEngine.Tests;

public sealed class ScenePlayerTests
{
    [Fact]
    public void RenderToString_EmptyScene_ReturnsNonEmptyString()
    {
        var scene = new SceneDefinition
        {
            Lines = [],
        };
        string result = ScenePlayer.RenderToString(scene);
        Assert.NotEmpty(result);
    }

    [Fact]
    public void RenderToString_WithTitle_IncludesTitleInOutput()
    {
        var scene = new SceneDefinition
        {
            Title = "Chapter One",
            Lines = [],
        };
        string result = ScenePlayer.RenderToString(scene);
        Assert.Contains("Chapter One", result);
    }

    [Fact]
    public void RenderToString_WithLines_IncludesLinesInOutput()
    {
        var scene = new SceneDefinition
        {
            Lines = ["First line", "Second line"],
        };
        string result = ScenePlayer.RenderToString(scene);
        Assert.Contains("First line", result);
        Assert.Contains("Second line", result);
    }

    [Fact]
    public void RenderToString_WithContinuePrompt_IncludesPromptInOutput()
    {
        var scene = new SceneDefinition
        {
            Lines          = [],
            PromptContinue = true,
            ContinuePrompt = ">> CONTINUE <<",
        };
        string result = ScenePlayer.RenderToString(scene);
        Assert.Contains(">> CONTINUE <<", result);
    }

    [Fact]
    public void RenderToString_NoContinuePrompt_DoesNotIncludeDefaultPrompt()
    {
        var scene = new SceneDefinition
        {
            Lines          = [],
            PromptContinue = false,
        };
        string result = ScenePlayer.RenderToString(scene);
        Assert.DoesNotContain("PRESS ENTER", result);
    }

    [Fact]
    public void RenderToString_WithAsciiArt_IncludesArtLines()
    {
        var scene = new SceneDefinition
        {
            Lines    = [],
            AsciiArt = ["  /\\  ", " /  \\ "],
        };
        string result = ScenePlayer.RenderToString(scene);
        Assert.Contains("/\\", result);
    }

    [Fact]
    public void RenderToString_ArtColorInOutput()
    {
        var scene = new SceneDefinition
        {
            Lines    = [],
            AsciiArt = ["==="],
            ArtColor = ConsoleColor.Red,
        };
        string result = ScenePlayer.RenderToString(scene);
        Assert.Contains("Red", result);
    }

    [Fact]
    public void RenderToString_WithSpritePath_IncludesSpriteNote()
    {
        var scene = new SceneDefinition
        {
            Lines      = [],
            SpritePath = "characters/hero.png",
        };
        string result = ScenePlayer.RenderToString(scene);
        Assert.Contains("hero.png", result);
    }

    [Fact]
    public void RenderToString_EmptyTitle_DoesNotCrash()
    {
        var scene = new SceneDefinition
        {
            Title = string.Empty,
            Lines = ["Some line."],
        };
        string result = ScenePlayer.RenderToString(scene);
        Assert.Contains("Some line.", result);
    }

    [Fact]
    public void RenderToString_EmptyAsciiArt_DoesNotCrash()
    {
        var scene = new SceneDefinition
        {
            Lines    = ["text"],
            AsciiArt = [],
        };
        string result = ScenePlayer.RenderToString(scene);
        Assert.Contains("text", result);
    }

    [Fact]
    public void RenderToString_ContinuePromptNull_DoesNotIncludeNull()
    {
        var scene = new SceneDefinition
        {
            Lines          = [],
            PromptContinue = true,
            ContinuePrompt = null,
        };
        string result = ScenePlayer.RenderToString(scene);
        Assert.DoesNotContain("null", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RenderToString_MultipleLines_AllPresent()
    {
        var scene = new SceneDefinition
        {
            Lines = ["Alpha", "Beta", "Gamma"],
        };
        string result = ScenePlayer.RenderToString(scene);
        Assert.Contains("Alpha", result);
        Assert.Contains("Beta",  result);
        Assert.Contains("Gamma", result);
    }
}
