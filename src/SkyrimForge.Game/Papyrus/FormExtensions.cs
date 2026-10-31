using SkyrimForge.Game;

namespace SkyrimForge.Papyrus;

/// <summary>Provides extension members for <see cref="IForm"/>.</summary>
public static class FormExtensions
{
    extension<T>(T self) where T : IForm
    {
        /// <summary>This form, with its Papyrus functions callable on it: <c>await actor.Papyrus.GetLevel()</c>.</summary>
        /// <remarks>
        /// A call runs in the Papyrus VM a frame or two later, and its task completes on the game's thread.
        /// The form's own declaration of the same function returns on the calling thread.
        /// </remarks>
        public PapyrusScript<T> Papyrus => new(self);
    }
}
