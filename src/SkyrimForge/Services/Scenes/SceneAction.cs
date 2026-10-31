using System.Collections.Generic;

namespace SkyrimForge;

/// <summary>One action of a <see cref="ScenePhase"/>, such as a <see cref="SceneLine"/>, a <see cref="SceneTimer"/>, or a <see cref="ScenePackage"/>.</summary>
public abstract record SceneAction
{
    private protected SceneAction(QuestReferenceAlias? performer)
    {
        Performer = performer;
    }

    /// <summary>The index of the last phase the action runs through. A null value ends the action in the phase whose actions include it.</summary>
    /// <remarks>The game waits for an action only in the phase it ends in, so a phase whose actions all end in later phases ends as it begins.</remarks>
    public int? EndPhaseIndex { get; init; }

    internal QuestReferenceAlias? Performer { get; }

    internal virtual IEnumerable<QuestReferenceAlias> Actors => Performer != null ? [Performer] : [];

    /// <remarks>Not a constructor parameter, since a <see cref="SceneLine"/>'s <see cref="SceneLine.LookTarget"/> is initialized after the constructor runs.</remarks>
    internal abstract IEnumerable<QuestReferenceAlias> Aliases { get; }

    internal virtual IEnumerable<QuestLocationAlias> LocationAliases => [];
}
