namespace SkyrimForge.Game;

/// <summary>Font names from Interface\fontconfig.txt. Each resolves to the font for the game's language.</summary>
public static class BookFonts
{
    /// <summary>The font of a printed book.</summary>
    public const string SkyrimBooks = "$SkyrimBooks";

    /// <summary>The font of a note written by hand.</summary>
    public const string HandwrittenFont = "$HandwrittenFont";

    /// <summary>The font of text the player cannot read, such as the Falmer script.</summary>
    public const string SkyrimBooks_UnreadableFont = "$SkyrimBooks_UnreadableFont";
}
