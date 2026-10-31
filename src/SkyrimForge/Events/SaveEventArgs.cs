namespace SkyrimForge;

/// <summary>The save that <see cref="IGameEvents.OnSaveLoading"/>, <see cref="IGameEvents.OnSaveWriting"/>, or <see cref="IGameEvents.OnSaveDeleted"/> concerns.</summary>
/// <param name="FileName">
/// The file's name, not a path, or null when SKSE sends no name. A read includes the extension, as in Save 1.ess, and a write or a deletion omits it.
/// </param>
public readonly record struct SaveEventArgs(string? FileName);
