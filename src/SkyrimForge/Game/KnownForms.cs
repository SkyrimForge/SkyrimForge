namespace SkyrimForge.Game;

/// <summary>Forms from Skyrim.esm, each named by its editor ID. Look one up with <c>Form</c> or <c>FormOrNull</c>.</summary>
public static class KnownForms
{
    /// <summary>The player's base form.</summary>
    public static readonly FormId<ActorBase> Player = new(0x7);

    /// <summary>The player's reference.</summary>
    public static readonly FormId<Actor> PlayerRef = new(0x14);

    /// <summary>The game's courier.</summary>
    public static readonly FormId<Actor> WICourierNPCRef = new(0x39FB7);

    /// <summary>The container whose items the game's courier delivers.</summary>
    public static readonly FormId<ObjectReference> WICourierContainerRef = new(0x39FB9);

    /// <summary>Gold, the game's currency.</summary>
    public static readonly FormId<MiscObject> Gold001 = new(0xF);

    /// <summary>A lockpick.</summary>
    public static readonly FormId<MiscObject> Lockpick = new(0xA);

    /// <summary>A marker, which the game does not show.</summary>
    public static readonly FormId<Static> XMarker = new(0x3B);

    /// <summary>A marker that states a heading, which the game does not show.</summary>
    public static readonly FormId<Static> XMarkerHeading = new(0x34);

    /// <summary>The global variable that stores the hour of the day.</summary>
    public static readonly FormId<GlobalVariable> GameHour = new(0x38);

    /// <summary>The faction the player belongs to.</summary>
    public static readonly FormId<Faction> PlayerFaction = new(0xDB1);

    /// <summary>The faction of the actors following the player.</summary>
    public static readonly FormId<Faction> CurrentFollowerFaction = new(0x5C84E);

    /// <summary>The keyword on every race of people, as distinct from creatures.</summary>
    public static readonly FormId<Keyword> ActorTypeNPC = new(0x13794);

    /// <summary>The keyword on an inhabited location, such as a city, a town, or a settlement.</summary>
    public static readonly FormId<Keyword> LocTypeHabitation = new(0x39793);

    /// <summary>The quest of the game's follower dialogue.</summary>
    public static readonly FormId<Quest> DialogueFollower = new(0x750BA);

    /// <summary>The quest that runs the game's courier.</summary>
    public static readonly FormId<Quest> WICourier = new(0x39F82);

    /// <summary>A book that looks like an ordinary note.</summary>
    public static readonly FormId<Book> dunLabyrinthianMazeClue = new(0x108160);

    /// <summary>The package of an actor waiting for a conversation to end.</summary>
    public static readonly FormId<Package> zzzDEFAULTzzzWaitForDialogue = new(0x1CC5A);
}
