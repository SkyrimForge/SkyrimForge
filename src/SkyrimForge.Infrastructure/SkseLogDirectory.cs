namespace SkyrimForge.Infrastructure;

internal sealed class SkseLogDirectory(string path) : ISkseLogDirectory
{
    /// <inheritdoc/>
    public string Path { get; } = path;
}
