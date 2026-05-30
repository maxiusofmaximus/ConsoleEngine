using System.Diagnostics.CodeAnalysis;

namespace ConsoleEngine.World;

/// <summary>
/// Immutable, indexed collection of <see cref="LocationDefinition"/> objects.
/// Provides O(1) look-ups by ID and direction-based neighbour resolution.
///
/// <b>Usage</b>
/// <code>
/// var map = new WorldMap(new[]
/// {
///     new LocationDefinition { Id = "town",   Exits = { ["north"] = "forest" } },
///     new LocationDefinition { Id = "forest", Exits = { ["south"] = "town"   } },
/// });
///
/// if (map.TryMove("town", "north", out var dest))
///     Console.WriteLine(dest.Name);
/// </code>
/// </summary>
public sealed class WorldMap
{
    private readonly Dictionary<string, LocationDefinition> _locations;

    /// <param name="locations">All locations in the world. IDs must be unique.</param>
    /// <exception cref="ArgumentException">Thrown if any two locations share the same ID.</exception>
    public WorldMap(IEnumerable<LocationDefinition> locations)
    {
        ArgumentNullException.ThrowIfNull(locations);
        _locations = new Dictionary<string, LocationDefinition>(StringComparer.OrdinalIgnoreCase);
        foreach (var loc in locations)
        {
            if (string.IsNullOrWhiteSpace(loc.Id))
                throw new ArgumentException("LocationDefinition.Id must not be empty.", nameof(locations));
            if (!_locations.TryAdd(loc.Id, loc))
                throw new ArgumentException($"Duplicate location ID: '{loc.Id}'.", nameof(locations));
        }
    }

    /// <summary>All locations in the map, in insertion order.</summary>
    public IReadOnlyCollection<LocationDefinition> AllLocations => _locations.Values;

    /// <summary>Returns the location with the given <paramref name="id"/>.</summary>
    /// <exception cref="KeyNotFoundException">Thrown if the ID does not exist in this map.</exception>
    public LocationDefinition Get(string id)
        => _locations.TryGetValue(id, out var loc) ? loc
           : throw new KeyNotFoundException($"Location '{id}' not found in WorldMap.");

    /// <summary>Tries to find a location by ID. Returns <c>false</c> if not found.</summary>
    public bool TryGet(string id, [NotNullWhen(true)] out LocationDefinition? location)
        => _locations.TryGetValue(id, out location);

    /// <summary>
    /// Tries to resolve movement from <paramref name="currentId"/> in <paramref name="direction"/>.
    /// Checks the current location's <see cref="LocationDefinition.Exits"/> dictionary.
    /// Returns <c>false</c> if the direction is not an exit, or if the destination ID is unknown.
    /// </summary>
    public bool TryMove(
        string currentId,
        string direction,
        [NotNullWhen(true)] out LocationDefinition? destination)
    {
        destination = null;
        if (!_locations.TryGetValue(currentId, out var current)) return false;
        if (!current.Exits.TryGetValue(direction, out string? destId)) return false;
        return _locations.TryGetValue(destId, out destination);
    }
}
