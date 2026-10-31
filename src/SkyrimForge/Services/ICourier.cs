using SkyrimForge.Game;
using System.Threading.Tasks;

namespace SkyrimForge;

/// <summary>Sends items to the player by the game's courier.</summary>
/// <remarks>
/// The courier delivers when the player is in a settlement. The framework then shows the name of each item delivered, including items the game and other mods sent.
/// Call members on the game's thread.
/// </remarks>
public interface ICourier
{
    /// <summary>Adds <paramref name="item"/> to the courier's container.</summary>
    /// <returns>A task that completes once the courier has the item.</returns>
    Task SendAsync<TItem>(TItem item) where TItem : IObjectReference;
}
