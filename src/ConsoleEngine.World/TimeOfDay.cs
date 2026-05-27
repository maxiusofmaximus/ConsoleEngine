namespace ConsoleEngine.World;

/// <summary>Time-of-day periods used by the exploration system.</summary>
public enum TimeOfDay
{
    /// <summary>Early hours; dawn to mid-morning.</summary>
    Morning,

    /// <summary>Mid-day hours.</summary>
    Afternoon,

    /// <summary>Late-day hours; dusk approaches.</summary>
    Evening,

    /// <summary>Night hours; rest or danger.</summary>
    Night,
}

/// <summary>Extension helpers for <see cref="TimeOfDay"/>.</summary>
public static class TimeOfDayExtensions
{
    /// <summary>Returns the next period in the cycle (Night wraps to Morning).</summary>
    public static TimeOfDay Next(this TimeOfDay t)
        => t == TimeOfDay.Night ? TimeOfDay.Morning : (TimeOfDay)((int)t + 1);

    /// <summary>Returns the <c>CK.Status.*</c> locale key for this period.</summary>
    public static string LocaleKey(this TimeOfDay t) => t switch
    {
        TimeOfDay.Morning   => ConsoleEngine.Locale.CK.Status.Morning,
        TimeOfDay.Afternoon => ConsoleEngine.Locale.CK.Status.Afternoon,
        TimeOfDay.Evening   => ConsoleEngine.Locale.CK.Status.Evening,
        TimeOfDay.Night     => ConsoleEngine.Locale.CK.Status.Night,
        _                   => ConsoleEngine.Locale.CK.Status.Morning,
    };
}
