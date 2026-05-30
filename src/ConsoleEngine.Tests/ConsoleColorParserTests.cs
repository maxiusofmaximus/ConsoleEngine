using ConsoleEngine.Core;
using Xunit;

namespace ConsoleEngine.Tests;

public sealed class ConsoleColorParserTests
{
    [Fact]
    public void Parse_ValidName_ReturnsColor()
    {
        Assert.Equal(ConsoleColor.Red, ConsoleColorParser.Parse("Red"));
    }

    [Fact]
    public void Parse_CaseInsensitive_DarkGreen()
    {
        Assert.Equal(ConsoleColor.DarkGreen, ConsoleColorParser.Parse("darkgreen"));
    }

    [Fact]
    public void Parse_MixedCase_ReturnsColor()
    {
        Assert.Equal(ConsoleColor.DarkBlue, ConsoleColorParser.Parse("DarkBlue"));
    }

    [Fact]
    public void Parse_Null_ReturnsFallback()
    {
        Assert.Equal(ConsoleColor.Cyan, ConsoleColorParser.Parse(null, ConsoleColor.Cyan));
    }

    [Fact]
    public void Parse_EmptyString_ReturnsFallback()
    {
        Assert.Equal(ConsoleColor.White, ConsoleColorParser.Parse("", ConsoleColor.White));
    }

    [Fact]
    public void Parse_UnknownName_ReturnsFallback()
    {
        Assert.Equal(ConsoleColor.Gray, ConsoleColorParser.Parse("NotAColor"));
    }

    [Fact]
    public void Parse_DefaultFallback_IsGray()
    {
        Assert.Equal(ConsoleColor.Gray, ConsoleColorParser.Parse(null));
    }

    [Fact]
    public void Parse_AllConsoleColors_RoundTrip()
    {
        foreach (ConsoleColor color in Enum.GetValues<ConsoleColor>())
        {
            var parsed = ConsoleColorParser.Parse(color.ToString(), ConsoleColor.Gray);
            Assert.Equal(color, parsed);
        }
    }
}
