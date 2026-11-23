using System.Reflection;

namespace ConsoleEngine.Core;

/// <summary>ConsoleEngine version, read from the assembly so it follows &lt;Version&gt; in Directory.Build.props.</summary>
public static class EngineVersion
{
    /// <summary>Semantic version without the SourceLink commit suffix (e.g. "0.7.4").</summary>
    public static readonly string Full = Resolve();

    public static readonly string Display = $"ConsoleEngine v{Full}";

    private static string Resolve()
    {
        var asm = typeof(EngineVersion).Assembly;
        var info = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrEmpty(info))
            return info.Split('+')[0];

        var v = asm.GetName().Version!;
        return $"{v.Major}.{v.Minor}.{Math.Max(v.Build, 0)}";
    }
}
