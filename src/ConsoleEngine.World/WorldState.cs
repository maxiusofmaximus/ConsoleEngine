namespace ConsoleEngine.World;

/// <summary>
/// Mutable snapshot of the player's position and time in the world.
/// Designed to be serialised with <c>ConsoleEngine.Persistence.SaveRepository&lt;T&gt;</c>.
///
/// <b>Usage</b>
/// <code>
/// var state = new WorldState { CurrentLocationId = "town", TimeOfDay = TimeOfDay.Morning, Day = 1 };
/// state.AdvanceTime(); // → Afternoon
/// state.AdvanceTime(3); // → Morning, Day 2  (wraps through Evening → Night → Morning)
/// </code>
/// </summary>
public sealed class WorldState
{
    /// <summary>ID of the location the player currently occupies.</summary>
    public string CurrentLocationId { get; set; } = string.Empty;

    /// <summary>Current time-of-day period.</summary>
    public TimeOfDay TimeOfDay { get; set; } = TimeOfDay.Morning;

    /// <summary>Current in-game day (1-based).</summary>
    public int Day { get; set; } = 1;

    /// <summary>
    /// Advances the time-of-day by <paramref name="steps"/> periods.
    /// Wrapping from <see cref="TimeOfDay.Night"/> to <see cref="TimeOfDay.Morning"/>
    /// increments <see cref="Day"/> by one.
    /// </summary>
    public void AdvanceTime(int steps = 1)
    {
        for (int i = 0; i < steps; i++)
        {
            if (TimeOfDay == TimeOfDay.Night)
            {
                TimeOfDay = TimeOfDay.Morning;
                Day++;
            }
            else
            {
                TimeOfDay = TimeOfDay.Next();
            }
        }
    }
}
