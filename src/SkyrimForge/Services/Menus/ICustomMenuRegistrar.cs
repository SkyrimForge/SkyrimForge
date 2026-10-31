using SkyrimForge.Game;
using System;

namespace SkyrimForge;

/// <summary>Registers a menu of the plugin's own, which the game then constructs, shows, and drives as it does its own.</summary>
/// <remarks>
/// <code>
/// menus.Register("SkyrimForge Menu", message => message switch
/// {
///     UI_MESSAGE_TYPE.kShow => UI_MESSAGE_RESULTS.kHandled,
///     _ => UI_MESSAGE_RESULTS.kPassOn
/// });
/// gameTarget.UIMessageQueue.AddMessage("SkyrimForge Menu", UI_MESSAGE_TYPE.kShow);
/// </code>
/// <para>An exception from a handler is written to the log of the plugin that registered it. Call members on the game's thread.</para>
/// </remarks>
public interface ICustomMenuRegistrar
{
    /// <summary>Registers a menu under <paramref name="name"/>.</summary>
    /// <param name="name">The name that opens and closes the menu.</param>
    /// <param name="handler">
    /// The handler for each message the game sends the menu. It may call the game.
    /// Only <see cref="UI_MESSAGE_RESULTS.kIgnore"/> changes what the game does, leaving a show unopened or a hide unclosed. The game does not resend an ignored message.
    /// </param>
    /// <param name="movieFile">
    /// The name of a Flash movie under Data/Interface without its extension, so that MyMenu loads Interface/MyMenu.swf.
    /// Null, or a name that loads no movie, gives a menu that draws nothing but still applies its input context.
    /// </param>
    /// <param name="flags">The effects the menu has on the game while it is open, such as pausing the game or capturing the cursor.</param>
    /// <param name="depth">The menu's rank against the others. A higher rank is nearer the front. The game's HUD is 2, its console 12, and its cursor 13.</param>
    /// <param name="context">Which control mapping is in force while the menu is open.</param>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="handler"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty, or the game already registers a menu under it.</exception>
    /// <exception cref="InvalidOperationException">The game has no interface yet, or 32 custom menus are already registered.</exception>
    void Register(string name, Func<UI_MESSAGE_TYPE, UI_MESSAGE_RESULTS> handler, string? movieFile = null, UI_MENU_FLAGS flags = UI_MENU_FLAGS.kNone, sbyte depth = 3, UserEvents.INPUT_CONTEXT_IDS.INPUT_CONTEXT_ID context = UserEvents.INPUT_CONTEXT_IDS.INPUT_CONTEXT_ID.kMenuMode);
}
