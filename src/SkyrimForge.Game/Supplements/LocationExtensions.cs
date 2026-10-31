using System.Collections.Generic;
using System.Linq;

namespace SkyrimForge.Game;

/// <summary>Provides extension members for <see cref="Location"/>.</summary>
public static class LocationExtensions
{
    extension(Location location)
    {
        /// <summary>The location and each location containing it, innermost first.</summary>
        /// <remarks>Call on the game's thread.</remarks>
        public IReadOnlyList<Location> GetSelfAndAncestors() => [location, .. location.GetAncestors()];

        /// <summary>The locations containing the location, innermost first.</summary>
        /// <remarks>Call on the game's thread.</remarks>
        public IReadOnlyList<Location> GetAncestors()
        {
            List<Location> ancestors = [];
            for (Location? current = location.ParentLoc; current != null; current = current.Value.ParentLoc)
            {
                ancestors.Add(current.Value);
            }
            return ancestors;
        }

        /// <summary>Determines whether the location has <paramref name="keyword"/>.</summary>
        /// <remarks>Call on the game's thread.</remarks>
        public bool HasKeyword<TKeyword>(TKeyword keyword) where TKeyword : IKeyword => location.AsBGSKeywordForm().HasKeyword(keyword);

        /// <summary>Whether the location is <paramref name="ancestorId"/> or sits inside it.</summary>
        /// <remarks>Call on the game's thread.</remarks>
        public bool IsWithin(FormId<Location> ancestorId) => location.GetSelfAndAncestors().Any(ancestor => ancestor.FormId == ancestorId);
    }
}
