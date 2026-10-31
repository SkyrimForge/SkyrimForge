using SkyrimForge.Game;
using System;
using System.Collections.Generic;

namespace SkyrimForge;

/// <summary>Creates quests that the journal lists, at runtime and without an .esp file.</summary>
/// <remarks>Each quest's progress is stored in the co-save and applied again after every load. Call members on the game's thread.</remarks>
public interface IRuntimeQuestFactory
{
    /// <summary>Creates a quest that the journal lists once it displays an objective.</summary>
    /// <param name="key">The name the quest's progress is stored under, unique among this plugin's quests and the same in every version of the plugin.</param>
    /// <param name="name">The name the journal shows.</param>
    /// <param name="stages">The stages <see cref="QuestStage.SetAsync"/> sets.</param>
    /// <param name="type">The journal's category for the quest.</param>
    /// <param name="scenes">The scenes <see cref="QuestScene.Start"/> starts. A scene a stage starts is declared without being passed here.</param>
    /// <param name="aliases">
    /// The aliases no objective targets and no scene action uses, such as one that gives an actor a package or one written only as a token into text.
    /// An alias an objective targets or a scene action uses is declared without being passed here.
    /// </param>
    /// <param name="activeOnStart">
    /// Whether the quest becomes the journal's active quest as it displays its first objective, including after <see cref="IRuntimeQuest.RemoveFromJournalAndReset"/>.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> or <paramref name="name"/> is <c>null</c>.</exception>
    /// <exception cref="System.IO.FileNotFoundException">A scene line's fixed response specifies a voice file that does not exist.</exception>
    /// <exception cref="ArgumentException">
    /// This plugin created a quest with <paramref name="key"/> already, or two of the quest's stages or objectives share an index, or two of its aliases or scenes share a name,
    /// or a stage both completes and fails one objective, or completes or fails an objective created with no index that no stage displays,
    /// or a scene or phase sets a stage the quest does not declare, or an alias, a scene, a stage, or an objective belongs to another quest.
    /// </exception>
    /// <remarks>Callable from a plugin's start, before the game has data or a save.</remarks>
    IRuntimeQuest Create(string key, string name, IEnumerable<QuestStage>? stages = null, QUEST_DATA.Type type = QUEST_DATA.Type.kSideQuest, IEnumerable<QuestScene>? scenes = null, IEnumerable<QuestAlias>? aliases = null,
        bool activeOnStart = false);
}
