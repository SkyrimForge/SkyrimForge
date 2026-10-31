namespace SkyrimForge.Infrastructure;

internal readonly record struct CoSaveScope(string PluginTypeName, uint? FormId, string[] Path);
