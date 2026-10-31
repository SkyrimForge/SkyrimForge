using SkyrimForge.Game;

namespace SkyrimForge;

/// <summary>One value of a story event.</summary>
public readonly record struct StoryEventValue
{
    internal StoryEventValue(string name, StoryEventValueKind kind, nint address, long number)
    {
        Name = name;
        Kind = kind;
        Address = address;
        Number = number;
    }

    /// <summary>The name the game gives this value, such as "Workbench" or "Crime Gold".</summary>
    public string Name { get; }

    /// <summary>
    /// The kind of value, which decides which of <see cref="Number"/>, <see cref="Reference"/>, <see cref="Form"/>, <see cref="Location"/>, and <see cref="Keyword"/> is set.
    /// </summary>
    public StoryEventValueKind Kind { get; }

    /// <summary>The number, and 0 for every other kind.</summary>
    public long Number { get; }

    internal nint Address { get; }

    /// <summary>The reference, and null if this value is of another kind or the game passed none.</summary>
    public ObjectReference? Reference => Kind == StoryEventValueKind.Reference ? ObjectReference.OrNull(Address) : null;

    /// <summary>The form, and null if this value is of another kind or the game passed none.</summary>
    public Form? Form => Kind == StoryEventValueKind.Form ? Game.Form.OrNull(Address) : null;

    /// <summary>The location, and null if this value is of another kind or the game passed none.</summary>
    public Location? Location => Kind == StoryEventValueKind.Location ? Game.Location.OrNull(Address) : null;

    /// <summary>The keyword, and null if this value is of another kind or the game passed none.</summary>
    public Keyword? Keyword => Kind == StoryEventValueKind.Keyword ? Game.Keyword.OrNull(Address) : null;
}
