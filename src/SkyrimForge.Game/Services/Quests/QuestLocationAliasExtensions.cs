using SkyrimForge.Game;

namespace SkyrimForge;

/// <summary>Provides extension members for <see cref="QuestLocationAlias"/>.</summary>
public static class QuestLocationAliasExtensions
{
    extension(QuestLocationAlias alias)
    {
        /// <summary>Fills the alias with <paramref name="location"/>, as <see cref="QuestLocationAlias.Fill(FormId{Location})"/> does with its form ID.</summary>
        /// <inheritdoc cref="QuestLocationAlias.Fill(FormId{Location})" path="/exception"/>
        public void Fill(Location location) => alias.Fill(location.FormId);
    }
}
