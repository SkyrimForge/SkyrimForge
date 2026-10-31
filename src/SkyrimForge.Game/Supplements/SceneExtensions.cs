using System.Linq;

namespace SkyrimForge.Game;

/// <summary>Provides extension members for <see cref="Scene"/>.</summary>
public static class SceneExtensions
{
    extension(Scene scene)
    {
        /// <summary>Returns whether the action whose index is <paramref name="actionId"/> is complete, as Papyrus's IsActionComplete does.</summary>
        /// <remarks>Returns <c>false</c> if the scene has no such action. Call on the game's thread.</remarks>
        public bool IsActionComplete(int actionId)
        {
            // The game reads the first action whose index, without its two high bits, equals the ID.
            const uint indexMask = 0x3FFFFFFF;
            return scene.Actions.Get().Where(action => (action.Index & indexMask) == (uint)actionId).Take(1).Any(action => action.ActiveFlags.HasFlag(BGSSceneAction.ActiveFlag.kComplete));
        }
    }
}
