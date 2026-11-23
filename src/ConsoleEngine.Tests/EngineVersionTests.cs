using ConsoleEngine.Core;
using Xunit;

namespace ConsoleEngine.Tests;

public sealed class EngineVersionTests
{
    [Fact]
    public void Full_Is_Parseable_Version_Not_Hardcoded_Old_Value()
    {
        Assert.True(Version.TryParse(EngineVersion.Full, out _));
        Assert.NotEqual("0.6.0", EngineVersion.Full);
    }

    [Fact]
    public void Display_Starts_With_Engine_Prefix()
    {
        Assert.StartsWith("ConsoleEngine v", EngineVersion.Display);
    }
}
