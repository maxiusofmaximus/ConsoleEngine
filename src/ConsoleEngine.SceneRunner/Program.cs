using ConsoleEngine.Scenes;

// ── ConsoleEngine.SceneRunner ─────────────────────────────────────────────
// Minimal console host launched by ConsoleEngine.Editor's Play button.
// Usage: ConsoleEngine.SceneRunner <path-to-scene.json>

if (args.Length == 0)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("ConsoleEngine.SceneRunner — missing argument");
    Console.WriteLine("Usage: ConsoleEngine.SceneRunner <path-to-scene.json>");
    Console.ResetColor();
    Pause();
    return 1;
}

string scenePath = args[0];

if (!File.Exists(scenePath))
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"Scene file not found: {scenePath}");
    Console.ResetColor();
    Pause();
    return 1;
}

try
{
    Console.OutputEncoding = System.Text.Encoding.UTF8;
    var scene = SceneLoader.Load(scenePath);
    ScenePlayer.Play(scene);
    return 0;
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"Error: {ex.Message}");
    Console.ResetColor();
    Pause();
    return 1;
}

static void Pause()
{
    Console.WriteLine();
    Console.Write("Press any key to close...");
    Console.ReadKey(intercept: true);
}
