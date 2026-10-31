using System.Collections.Generic;

namespace SkyrimForge.Game;

internal sealed record SceneHostQuest(Quest Quest, QuestRecordAliases Aliases, IReadOnlyList<QuestReferenceAlias> NamedAliases);
