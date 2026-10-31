namespace SkyrimForge;

/// <summary>The source a <see cref="SpawnPosition"/> was found in, in the order <see cref="IPlayerPositionHistory.FindSpawnPosition"/> tests them.</summary>
public enum SpawnPositionSource
{
    /// <summary>A position the player stood on.</summary>
    History,

    /// <summary>One of the candidates the plugin passed.</summary>
    Candidate,

    /// <summary>The ground under a map marker.</summary>
    MapMarker,

    /// <summary>The ground behind the player.</summary>
    BehindPlayer
}
