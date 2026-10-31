using System;
using System.Collections.Generic;

namespace SkyrimForge.Game;

/// <summary>Provides extension members for <see cref="ReadableGameList{T}"/>.</summary>
public static class ReadableGameListExtensions
{
    extension<T>(ReadableGameList<T> list) where T : struct
    {
        /// <summary>Returns a copy of every entry of the container, a null one included, so an index into the list is an index into the container.</summary>
        /// <exception cref="NotSupportedException">The container stores handles or is a map.</exception>
        public IReadOnlyList<T?> GetSlots() => list.ReadSlots<T>();
    }
}
