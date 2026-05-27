using ConsoleEngine.World;

namespace WorldDemo;

/// <summary>
/// Provides the <see cref="WorldMap"/> for the WorldDemo sample by loading
/// <c>GameData/world/world.world.json</c> at runtime via <see cref="WorldLoader"/>.
///
/// Layout:
/// <code>
///        [hilltop]
///            |
///        [forest]
///            |
///   [ruins]──[crossroads]
///     |
///  [cave]
/// </code>
/// </summary>
internal static class TheWorld
{
    public static WorldMap Build(string gameDataRoot) =>
        WorldLoader.Load(Path.Combine(gameDataRoot, "world", "world.world.json"));
}
