namespace SkyrimForge;

/// <summary>The type of one value of a story event.</summary>
public enum StoryEventValueKind
{
    /// <summary>A reference.</summary>
    Reference = 0,

    /// <summary>A form of any kind.</summary>
    Form = 1,

    /// <summary>A location.</summary>
    Location = 2,

    /// <summary>A number.</summary>
    Number = 3,

    /// <summary>A keyword.</summary>
    Keyword = 4
}
