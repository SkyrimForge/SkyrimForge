namespace SkyrimForge.Game;

/// <summary>Provides extension members for <see cref="ImageSpaceModifier"/>.</summary>
public static class ImageSpaceModifierExtensions
{
    extension(ImageSpaceModifier modifier)
    {
        /// <summary>Applies the modifier at <paramref name="strength"/>, as Papyrus's Apply does.</summary>
        /// <remarks>Call on the game's thread.</remarks>
        public void Apply(float strength = 1f) => ImageSpaceModifierInstanceForm.Trigger(modifier, strength);

        /// <summary>Removes the modifier, as Papyrus's Remove does.</summary>
        /// <remarks>Call on the game's thread.</remarks>
        public void Remove() => ImageSpaceModifierInstanceForm.Stop(modifier);

        /// <summary>Removes the modifier and applies <paramref name="newModifier"/> at <paramref name="strength"/>, as Papyrus's PopTo does.</summary>
        /// <remarks>Call on the game's thread.</remarks>
        public void PopTo(ImageSpaceModifier newModifier, float strength = 1f)
        {
            ImageSpaceModifierInstanceForm.Stop(modifier);
            ImageSpaceModifierInstanceForm.Trigger(newModifier, strength);
        }
    }
}
