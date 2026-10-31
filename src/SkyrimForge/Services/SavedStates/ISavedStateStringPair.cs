namespace SkyrimForge;

/// <summary>One text entry in a saved-state store.</summary>
/// <remarks>Members can be called on any thread.</remarks>
public interface ISavedStateStringPair
{
    /// <summary>The key this reads and writes within its store.</summary>
    string Key { get; }

    /// <summary>The stored text, or null if nothing is stored under <see cref="Key"/>. Assigning null drops the key.</summary>
    string? Value { get; set; }
}
