using System;

namespace SkyrimForge;

/// <summary>A named placeholder of a quest, the base of <see cref="QuestReferenceAlias"/> and <see cref="QuestLocationAlias"/>.</summary>
/// <remarks>
/// A quest declares the aliases its objectives target and its scene actions use, and those passed in <c>aliases</c> to <see cref="IRuntimeQuestFactory.Create"/>.
/// Call its members on the game's thread. Filling, clearing, and reading what fills an alias need a game loaded.
/// Subscribing to its events and adding a package to it do not.
/// </remarks>
public abstract class QuestAlias
{
    private protected QuestAlias(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        Name = name;
    }

    /// <summary>The alias's name.</summary>
    public string Name { get; }

    /// <summary>The quest the alias belongs to.</summary>
    /// <exception cref="InvalidOperationException">
    /// No quest that declares the alias has been created, or the alias is <see cref="QuestReferenceAlias.Player"/>.
    /// </exception>
    public IRuntimeQuest Quest => QuestAliases.Quest;

    internal IQuestAliases? QuestAliasesOrNull { get; set; }

    private protected IQuestAliases QuestAliases => QuestAliasesOrNull ?? throw new InvalidOperationException($"The alias \"{Name}\" belongs to no quest. "
        + "A quest declares the aliases its objectives target and its scene actions use, and those passed in aliases as it is created. "
        + $"{nameof(QuestReferenceAlias)}.{nameof(QuestReferenceAlias.Player)} belongs to none.");

    /// <summary>
    /// Returns the token <c>&lt;Alias=Name&gt;</c>,
    /// which the quest replaces in an objective's text or a stage's log entry with the name of the reference or location that fills the alias.
    /// </summary>
    public override string ToString() => $"<Alias={Name}>";
}
