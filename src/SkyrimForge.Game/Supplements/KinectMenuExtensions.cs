using System.Runtime.InteropServices;

namespace SkyrimForge.Game;

/// <summary>Provides extension members for <see cref="KinectMenu"/>.</summary>
public static class KinectMenuExtensions
{
    extension(KinectMenu menu)
    {
        /// <summary>Writes the menu's root into <paramref name="root"/>, which the caller allocated.</summary>
        /// <remarks>Disposing the <see cref="ScaleformValue"/> that stores <paramref name="root"/> releases the reference this writes into it.</remarks>
        public void GetRoot(GFxValue root) => NativeSupplements.SF_KinectMenu_GetRoot(menu.Address, root.Address);

        /// <summary>Makes <paramref name="root"/> the menu's root. The menu adds its own reference.</summary>
        public void SetRoot(GFxValue root) => NativeSupplements.SF_KinectMenu_SetRoot(menu.Address, root.Address);
    }
}

internal static partial class NativeSupplements
{
    [LibraryImport(library)]
    internal static partial void SF_KinectMenu_GetRoot(nint self, nint destination);

    [LibraryImport(library)]
    internal static partial void SF_KinectMenu_SetRoot(nint self, nint root);
}
