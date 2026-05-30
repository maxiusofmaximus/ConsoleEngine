namespace ConsoleEngine.Terminal;

/// <summary>
/// Spawn parameters for an <see cref="ITerminalBackend"/> session.
/// </summary>
/// <param name="Command">Executable to run (resolved via PATH), e.g. <c>"claude"</c> or <c>"cmd.exe"</c>.</param>
/// <param name="Args">Arguments passed to the command.</param>
/// <param name="WorkingDirectory">Initial working directory for the child process.</param>
/// <param name="Cols">Initial terminal width in columns.</param>
/// <param name="Rows">Initial terminal height in rows.</param>
/// <param name="Env">
/// Extra environment variables merged over the parent environment.
/// Typically includes <c>TERM=xterm-256color</c> and <c>FORCE_COLOR=1</c> so the child emits colour.
/// </param>
public sealed record TerminalOptions(
    string Command,
    string[] Args,
    string WorkingDirectory,
    int Cols = 80,
    int Rows = 24,
    IReadOnlyDictionary<string, string>? Env = null)
{
    /// <summary>Default environment that hints colour + xterm capabilities to the child.</summary>
    public static IReadOnlyDictionary<string, string> DefaultEnv { get; } =
        new Dictionary<string, string>
        {
            ["TERM"]        = "xterm-256color",
            ["FORCE_COLOR"] = "1",
            ["COLORTERM"]   = "truecolor",
        };
}
