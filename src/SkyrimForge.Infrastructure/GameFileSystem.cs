using SkyrimForge.Game.BSResource;
using System;
using System.IO;

namespace SkyrimForge.Infrastructure;

internal sealed class GameFileSystem : IGameFileSystem
{
    /// <inheritdoc/>
    public bool Exists(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        return NativeGlobals.SF_GameFileSystem_Exists(GetGamePath(path));
    }

    /// <inheritdoc/>
    public unsafe byte[] ReadAllBytes(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        ErrorCode error = NativeGlobals.SF_GameFileSystem_ReadAll(GetGamePath(path), out nint buffer, out nint data, out ulong length);
        if (error == ErrorCode.kNotExist)
        {
            throw new FileNotFoundException($"The game found no file at {path}.", path);
        }
        if (error != ErrorCode.kNone)
        {
            throw new IOException($"The game returned {error} reading {path}.");
        }
        try
        {
            return new ReadOnlySpan<byte>((void*)data, checked((int)length)).ToArray();
        }
        finally
        {
            NativeGlobals.SF_GameFileSystem_Release(buffer);
        }
    }

    private static string GetGamePath(string path) => path.Replace('/', '\\');
}
