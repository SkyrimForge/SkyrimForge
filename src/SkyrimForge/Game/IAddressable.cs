namespace SkyrimForge.Game;

/// <summary>A typed pointer to an object in the game's memory.</summary>
public interface IAddressable
{
    internal nint Address { get; }
}
