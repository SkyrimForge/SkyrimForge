using SkyrimForge.Game;

namespace SkyrimForge;

/// <summary>Arguments for <see cref="IReadableNotes.OnRead"/>.</summary>
/// <param name="Note">The note the book menu shows, activated in the world or read from the inventory.</param>
public readonly record struct NoteReadEventArgs(ObjectReference Note);
