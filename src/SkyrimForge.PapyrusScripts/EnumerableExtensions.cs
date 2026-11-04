using System.Collections.Generic;
using System.Linq;

namespace SkyrimForge.PapyrusScripts;

internal static class EnumerableExtensions
{
    extension<T>(IEnumerable<T?> source) where T : class
    {
        /// <summary>Removes the <c>null</c> entries.</summary>
        internal IEnumerable<T> WhereNotNull() => source.Where(value => value != null)!;
    }
}
