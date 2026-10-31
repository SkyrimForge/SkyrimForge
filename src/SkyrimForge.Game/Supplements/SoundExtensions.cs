using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace SkyrimForge.Game;

/// <summary>Provides extension members for <see cref="Sound"/>.</summary>
public static class SoundExtensions
{
    // The flags Papyrus's Play passes to GetSoundHandle.
    private const uint playFlags = 0x10;

    // The flags Papyrus's PlayAndWait passes to GetSoundHandle.
    private const uint playAndWaitFlags = 0x11;

    private static readonly TimeSpan startLimit = TimeSpan.FromSeconds(1);

    extension(Sound sound)
    {
        /// <summary>Plays the sound from <paramref name="source"/>, as Papyrus's Play does.</summary>
        /// <returns>The playback, or <c>null</c> if the game did not start the sound.</returns>
        /// <exception cref="InvalidOperationException"><paramref name="source"/> has no 3D.</exception>
        /// <remarks>Call on the game's thread.</remarks>
        public SoundInstance? Play<TSource>(BSAudioManager audioManager, TSource source) where TSource : IObjectReference
        {
            if (Prepare(sound, audioManager, source, playFlags) is { } handle)
            {
                try
                {
                    return handle.Play() ? new SoundInstance(handle.SoundID) : null;
                }
                finally
                {
                    // The sound continues to play once the handle is deleted.
                    handle.Delete();
                }
            }
            return null;
        }

        /// <summary>Plays the sound from <paramref name="source"/> and waits for it to finish, as Papyrus's PlayAndWait does.</summary>
        /// <returns>True once the sound has finished, and false if the game did not start it.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="utility"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="source"/> has no 3D. The exception is stored in the returned task.</exception>
        /// <remarks>Canceling leaves the sound playing. A new game or a save load cancels the wait. Call on the game's thread.</remarks>
        public Task<bool> PlayAndWaitAsync<TSource>(BSAudioManager audioManager, IUtility utility, TSource source, CancellationToken cancellationToken = default)
            where TSource : IObjectReference
        {
            ArgumentNullException.ThrowIfNull(utility);
            return Core();

            async Task<bool> Core()
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (Prepare(sound, audioManager, source, playAndWaitFlags) is { } handle)
                {
                    try
                    {
                        if (!handle.Play())
                        {
                            return false;
                        }
                        // The audio manager's thread registers a sound after Play returns, and IsPlaying returns false until then.
                        var sinceStart = Stopwatch.StartNew();
                        while (!handle.IsPlaying() && sinceStart.Elapsed < startLimit)
                        {
                            await utility.WaitAsync(TimeSpan.Zero, cancellationToken);
                        }
                        while (handle.IsPlaying())
                        {
                            await utility.WaitAsync(TimeSpan.Zero, cancellationToken);
                        }
                        return true;
                    }
                    finally
                    {
                        handle.Delete();
                    }
                }
                return false;
            }
        }
    }

    private static BSSoundHandle? Prepare<TSource>(Sound sound, BSAudioManager audioManager, TSource source, uint flags) where TSource : IObjectReference
    {
        if (source.GetCurrent3D() is not { } node)
        {
            throw new InvalidOperationException("The source reference has no loaded 3D.");
        }
        if (!sound.IsInitialized())
        {
            sound.InitItem();
        }
        if (sound.Descriptor is { } descriptor)
        {
            BSSoundHandle handle = BSSoundHandle.New();
            audioManager.GetSoundHandle(handle, descriptor.AsBSISoundDescriptor(), flags);
            if (handle.SetPosition(source.GetPosition()))
            {
                handle.SetObjectToFollow(node);
                return handle;
            }
            handle.Delete();
        }
        return null;
    }
}
