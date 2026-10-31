using System;
using System.Collections.Generic;
using System.Linq;

namespace SkyrimForge;

/// <summary>Provides extension members for <see cref="IEnumerable{T}"/>.</summary>
public static class EnumerableExtensions
{
    extension<T>(IEnumerable<T?> source)
    {
        /// <summary>Removes the <c>null</c> entries.</summary>
        public IEnumerable<T> WhereNotNull() => source.Where(value => value != null)!;
    }

    extension<T>(IEnumerable<T?> source) where T : struct
    {
        /// <summary>Removes the empty entries and reads the <c>Value</c> of the rest.</summary>
        public IEnumerable<T> WhereNotNull() => source.Where(value => value != null).Select(value => value!.Value);
    }

    extension<T>(IEnumerable<T> source) where T : struct
    {
        /// <summary>
        /// Returns the first entry, or <c>null</c> for a sequence with none. <c>FirstOrDefault</c> returns a default struct there,
        /// which for a view has no address.
        /// </summary>
        public T? FirstOrNull()
        {
            foreach (T value in source)
            {
                return value;
            }
            return null;
        }

        /// <summary>Returns the first entry <paramref name="predicate"/> accepts, or <c>null</c> if it accepts none.</summary>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is <c>null</c>.</exception>
        public T? FirstOrNull(Func<T, bool> predicate) => source.Where(predicate).FirstOrNull();
    }
}
