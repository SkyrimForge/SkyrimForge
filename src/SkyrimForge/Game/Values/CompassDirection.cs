namespace SkyrimForge.Game;

/// <summary>The eight compass directions <see cref="NiPoint3.GetCompassDirection"/> returns, clockwise from north, where +Y is north and +X is east.</summary>
public enum CompassDirection
{
    /// <summary>+Y.</summary>
    North,

    /// <summary>Between +Y and +X.</summary>
    Northeast,

    /// <summary>+X.</summary>
    East,

    /// <summary>Between -Y and +X.</summary>
    Southeast,

    /// <summary>-Y.</summary>
    South,

    /// <summary>Between -Y and -X.</summary>
    Southwest,

    /// <summary>-X.</summary>
    West,

    /// <summary>Between +Y and -X.</summary>
    Northwest
}
