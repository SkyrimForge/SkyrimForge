using SkyrimForge.Infrastructure;

namespace SkyrimForge.PluginHost;

/// <summary>The <c>ISingletonSource</c> implementation is chosen here so that the public <see cref="GameTarget"/> references no implementation.</summary>
internal static class GameTargetFactory
{
    public static GameTarget Create()
    {
        return new GameTarget(new SingletonSource());
    }
}
