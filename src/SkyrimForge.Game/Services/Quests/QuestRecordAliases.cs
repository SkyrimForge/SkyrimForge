using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace SkyrimForge.Game;

// A started quest looks up the alias object for each alias ID in its objectives' targets, and the process ends if it finds none.
// CommonLib declares no function that empties an alias object, so one whose alias is emptied remains filled with its last reference.
internal sealed class QuestRecordAliases
{
    private readonly GameTarget gameTarget;
    private readonly Quest record;

    // The index of each alias object is its alias ID.
    private readonly List<ReferenceAlias> aliasObjects = [];

    private readonly Dictionary<QuestReferenceAlias, uint> referenceAliasIds = [];
    private readonly Dictionary<QuestLocationAlias, uint> locationAliasIds = [];
    private readonly Dictionary<(ushort Objective, int Position), uint> referenceTargetIds = [];

    // Forcing an alias object to its current reference adds no instance, so Force adds one if RemoveInstance removed it.
    private readonly HashSet<uint> removedInstanceIds = [];

    internal QuestRecordAliases(GameTarget gameTarget, Quest record, QuestDeclaration declaration) : this(gameTarget, record, declaration.Aliases, declaration.LocationAliases, true)
    {
    }

    // A scene's record sets no quest object flag, since only the quest removes the instance as its alias empties.
    internal QuestRecordAliases(GameTarget gameTarget, Quest record, IEnumerable<QuestReferenceAlias> aliases) : this(gameTarget, record, aliases, [], false)
    {
    }

    private QuestRecordAliases(GameTarget gameTarget, Quest record, IEnumerable<QuestReferenceAlias> aliases, IEnumerable<QuestLocationAlias> locationAliases, bool marksQuestObjects)
    {
        this.gameTarget = gameTarget;
        this.record = record;
        foreach (QuestReferenceAlias alias in aliases)
        {
            referenceAliasIds.Add(alias, Create(alias.Name, marksQuestObjects && alias.IsQuestObject ? Alias.FLAGS.kQuestObject : Alias.FLAGS.kNone));
        }
        foreach (QuestLocationAlias alias in locationAliases)
        {
            locationAliasIds.Add(alias, Create(alias.Name, Alias.FLAGS.kNone));
        }
    }

    internal uint GetId(QuestReferenceAlias alias) => referenceAliasIds[alias];

    internal uint GetId(QuestLocationAlias alias) => locationAliasIds[alias];

    internal uint GetId(ushort objective, int position)
    {
        if (!referenceTargetIds.TryGetValue((objective, position), out uint id))
        {
            id = Create($"Objective {objective} target {position}", Alias.FLAGS.kNone);
            referenceTargetIds.Add((objective, position), id);
        }
        return id;
    }

    internal void Force(uint id, ObjectReference reference)
    {
        ReferenceAlias alias = aliasObjects[(int)id];
        if (alias.GetReference()?.Address != reference.Address)
        {
            removedInstanceIds.Remove(id);
            alias.ForceRefTo(reference);
        }
        else if (removedInstanceIds.Remove(id))
        {
            NativeSupplements.SF_AliasInstance_Add(reference.Address, record.Address, alias.Address);
        }
    }

    internal void SetPackage(uint id, ObjectReference reference, Package? package) =>
        NativeSupplements.SF_AliasInstance_SetPackage(reference.Address, record.Address, aliasObjects[(int)id].Address, package?.Address ?? 0);

    internal void RemoveInstance(uint id, ObjectReference reference)
    {
        NativeSupplements.SF_AliasInstance_Remove(reference.Address, record.Address, aliasObjects[(int)id].Address);
        removedInstanceIds.Add(id);
    }

    private uint Create(string name, Alias.FLAGS flags)
    {
        // The size CommonLib asserts for BGSRefAlias.
        const int refAliasSize = 0x48;
        uint id = (uint)aliasObjects.Count;
        nint address = GameHeap.AllocateObject(gameTarget, refAliasSize, "BGSRefAlias");
        Alias alias = Alias.OrNull(address)!.Value;
        alias.AliasName = name;
        alias.OwningQuest = record;
        alias.AliasID = id;
        alias.FillType = Alias.FILL_TYPE.kForced;
        // Without kOptional a quest whose alias is empty does not start, and without kAllowReserved an alias does not fill with an actor another quest's alias reserves.
        alias.Flags = Alias.FLAGS.kOptional | Alias.FLAGS.kAllowReserved | flags;
        record.Aliases.Add(alias);
        aliasObjects.Add(ReferenceAlias.OrNull(address)!.Value);
        return id;
    }
}

internal static partial class NativeSupplements
{
    [LibraryImport(library)]
    internal static partial void SF_AliasInstance_SetPackage(nint reference, nint quest, nint alias, nint package);

    [LibraryImport(library)]
    internal static partial void SF_AliasInstance_Add(nint reference, nint quest, nint alias);

    [LibraryImport(library)]
    internal static partial void SF_AliasInstance_Remove(nint reference, nint quest, nint alias);
}
