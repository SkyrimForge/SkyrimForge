using SkyrimForge.Game;

namespace SkyrimForge;

/// <summary>Provides extension members for <see cref="QuestReferenceAlias"/>.</summary>
public static class QuestReferenceAliasExtensions
{
    extension(QuestReferenceAlias alias)
    {
        /// <summary>Fills the alias with <paramref name="reference"/>, as <see cref="QuestReferenceAlias.Fill{TReference}(FormId{TReference})"/> does with its form ID.</summary>
        /// <inheritdoc cref="QuestReferenceAlias.Fill{TReference}(FormId{TReference})" path="/exception"/>
        public void Fill<TReference>(TReference reference) where TReference : struct, IObjectReference => alias.Fill(reference.FormId);
    }
}
