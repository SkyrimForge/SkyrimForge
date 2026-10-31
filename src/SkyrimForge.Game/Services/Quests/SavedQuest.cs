using System.Collections.Generic;

namespace SkyrimForge.Game;

internal sealed record SavedQuest(bool Completed, bool Failed, IReadOnlyList<SavedObjective> Objectives, string Description, bool Active, IReadOnlyList<ushort> Stages, IReadOnlyDictionary<ushort, string> ObjectiveTexts)
{
    // Properties rather than constructor parameters, so a record stored without them reads them as empty.
    public IReadOnlyDictionary<string, SavedAlias> Aliases { get; init; } = new Dictionary<string, SavedAlias>();

    public IReadOnlyDictionary<string, FormId<Location>> LocationAliases { get; init; } = new Dictionary<string, FormId<Location>>();

    public IReadOnlyDictionary<string, int> Scenes { get; init; } = new Dictionary<string, int>();

    public ScenePlayerControls DisabledControls { get; init; }

    public bool PlayerDriven { get; init; }

    public IReadOnlyList<string> SaidLines { get; init; } = [];
    public IReadOnlyDictionary<ushort, IReadOnlyList<FormId<ObjectReference>>> ObjectiveTargets { get; init; } = new Dictionary<ushort, IReadOnlyList<FormId<ObjectReference>>>();
}
