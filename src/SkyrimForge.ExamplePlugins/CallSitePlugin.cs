namespace SkyrimForge.ExamplePlugins;

/// <summary>Finds MainLoop's call to Main::Update by its offset into MainLoop on each edition, and logs its address.</summary>
/// <remarks>
/// An offset is valid for one game build only. <see cref="IFunctionHooks.FindCallSite(GameFunction, GameFunction, int, int)"/> works across builds,
/// but it does not find a call another plugin has replaced, and SkyrimForge replaces this one.
/// </remarks>
public sealed class CallSitePlugin(IFunctionHooks functionHooks, ILog log) : IPlugin
{
    /// <inheritdoc/>
    public void Start()
    {
        nint site = functionHooks.FindCallSite(GameFunction.MainLoop, new GameOffset(0x11F, 0x160));
        log.Write($"MainLoop calls Main::Update at 0x{site:X}.");
    }
}
