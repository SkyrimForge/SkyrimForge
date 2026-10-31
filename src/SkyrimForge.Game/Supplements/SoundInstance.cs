using System;

namespace SkyrimForge.Game;

/// <summary>A playback the game started, which Sound's Play and ObjectReference's PlaySoundFile return.</summary>
public readonly record struct SoundInstance
{
    internal SoundInstance(uint id)
    {
        Id = id;
    }

    /// <summary>The game's ID of the playback, which Papyrus's Sound.Play returns as an int.</summary>
    public uint Id { get; }

    /// <summary>Stops the playback, as Papyrus's Sound.StopInstance does.</summary>
    /// <remarks>Call on the game's thread.</remarks>
    public void Stop() => WithHandle(handle => handle.Stop());

    /// <summary>Sets the playback's volume, as Papyrus's Sound.SetInstanceVolume does.</summary>
    /// <param name="volume">The volume, from 0 to 1.</param>
    /// <remarks>Call on the game's thread.</remarks>
    public void SetVolume(float volume) => WithHandle(handle => handle.SetVolume(volume));

    private void WithHandle(Action<BSSoundHandle> action)
    {
        BSSoundHandle handle = BSSoundHandle.New();
        try
        {
            handle.SoundID = Id;
            action(handle);
        }
        finally
        {
            handle.Delete();
        }
    }
}
