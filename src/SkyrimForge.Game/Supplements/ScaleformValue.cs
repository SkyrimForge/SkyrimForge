using System;

namespace SkyrimForge.Game;

/// <summary>
/// A Scaleform value a plugin allocates and frees. Most members of <c>GFxValue</c> and <c>GFxMovie</c> write into a value the caller passes.
/// </summary>
/// <remarks>Dispose it on the game's thread or inside an <see cref="ITaskQueue"/> task.</remarks>
public sealed class ScaleformValue : IDisposable
{
    private nint address;

    private ScaleformValue(nint address) => this.address = address;

    /// <summary>Allocates a value that <see cref="Dispose"/> frees.</summary>
    /// <exception cref="InvalidOperationException">The value could not be allocated.</exception>
    public static ScaleformValue Create()
    {
        nint createdAddress = NativeBindings.SF_GFxValue_New();
        if (createdAddress == 0)
        {
            throw new InvalidOperationException("A Scaleform value could not be allocated.");
        }
        return new ScaleformValue(createdAddress);
    }

    /// <summary>The stored value as a <see cref="GFxValue"/>.</summary>
    public GFxValue Value
    {
        get
        {
            ObjectDisposedException.ThrowIf(address == 0, this);
            return GFxValue.FromAddress(address);
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (address != 0)
        {
            NativeBindings.SF_GFxValue_Delete(address);
            address = 0;
        }
    }
}
