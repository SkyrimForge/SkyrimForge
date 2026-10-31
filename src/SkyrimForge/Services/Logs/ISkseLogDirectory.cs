namespace SkyrimForge;

/// <summary>The folder that receives SKSE's logs for the running game, SkyrimForge's own log among them.</summary>
public interface ISkseLogDirectory
{
    /// <summary>
    /// The folder's full path, as SKSE reports it: Documents\My Games\[Edition]\SKSE,
    /// where [Edition] is Skyrim Special Edition, Skyrim Special Edition GOG, or Skyrim VR.
    /// </summary>
    string Path { get; }
}
