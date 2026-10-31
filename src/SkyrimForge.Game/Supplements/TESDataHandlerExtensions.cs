using SkyrimForge.Papyrus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace SkyrimForge.Game;

/// <summary>Provides extension members for <see cref="TESDataHandler"/>.</summary>
public static class TESDataHandlerExtensions
{
    // The CompileIndex of a file the game found but did not load.
    private const byte unloadedCompileIndex = 0xFF;

    extension(TESDataHandler dataHandler)
    {
        /// <summary>
        /// Gets the .esm and .esp files that are not light, in load order.
        /// </summary>
        /// <remarks>Call on the game's thread.</remarks>
        public IReadOnlyList<TESFile> GetLoadedMods()
        {
            return Sequences.Read((ref nint first, int capacity) => NativeSupplements.SF_TESDataHandler_GetLoadedMods(dataHandler.Address, ref first, capacity), TESFile.OrNull);
        }

        /// <summary>
        /// Gets the light .esm, .esp, and .esl files, which <see cref="GetLoadedMods"/> leaves out. On Skyrim VR the result is empty unless SkyrimVRESL is installed.
        /// </summary>
        /// <remarks>Call on the game's thread.</remarks>
        public IReadOnlyList<TESFile> GetLoadedLightMods()
        {
            return Sequences.Read((ref nint first, int capacity) => NativeSupplements.SF_TESDataHandler_GetLoadedLightMods(dataHandler.Address, ref first, capacity), TESFile.OrNull);
        }

        /// <summary>Whether the game loaded the .esm, .esp, or .esl file, light or not, named <paramref name="fileName"/>, such as Dawnguard.esm.</summary>
        /// <exception cref="ArgumentNullException"><paramref name="fileName"/> is <c>null</c>.</exception>
        /// <remarks>Call on the game's thread.</remarks>
        public bool IsFileLoaded(string fileName)
        {
            ArgumentNullException.ThrowIfNull(fileName);
            return dataHandler.LookupModByName(fileName) is { CompileIndex: not unloadedCompileIndex };
        }

        /// <summary>Gets the ID that the form <paramref name="localFormId"/> of the file <paramref name="fileName"/> has in the running load order.</summary>
        /// <param name="fileName">The name of the .esm, .esp, or .esl file that declares the form, such as Skyrim.esm.</param>
        /// <param name="localFormId">The form's ID without the file's load-order index.</param>
        /// <exception cref="ArgumentNullException"><paramref name="fileName"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="localFormId"/> exceeds 0xFFFFFF, or 0xFFF in a light file.</exception>
        /// <exception cref="InvalidOperationException">No loaded file is named <paramref name="fileName"/>.</exception>
        /// <remarks>Call on the game's thread.</remarks>
        public FormId<TForm> GetFormId<TForm>(string fileName, uint localFormId) where TForm : struct, IForm =>
            dataHandler.GetFormIdOrNull<TForm>(fileName, localFormId) ?? throw new InvalidOperationException($"No loaded file is named \"{fileName}\".");

        /// <summary>
        /// Gets the ID that the form <paramref name="localFormId"/> of the file <paramref name="fileName"/> has in the running load order,
        /// or <c>null</c> if no loaded file has that name.
        /// </summary>
        /// <inheritdoc cref="extension(TESDataHandler).GetFormId{TForm}(string, uint)" path="/param"/>
        /// <exception cref="ArgumentNullException"><paramref name="fileName"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="localFormId"/> exceeds 0xFFFFFF, or 0xFFF in a light file.</exception>
        /// <remarks>Call on the game's thread.</remarks>
        public FormId<TForm>? GetFormIdOrNull<TForm>(string fileName, uint localFormId) where TForm : struct, IForm
        {
            const uint maximumLocalFormId = 0xFFFFFF;
            const uint maximumLightLocalFormId = 0xFFF;
            const byte lightCompileIndex = 0xFE;
            ArgumentNullException.ThrowIfNull(fileName);
            // CommonLib adds the local ID to the file's index bits without masking it, so an ID past the file's range would identify a form of another file.
            ArgumentOutOfRangeException.ThrowIfGreaterThan(localFormId, maximumLocalFormId);
            if (dataHandler.LookupModByName(fileName) is { CompileIndex: not unloadedCompileIndex } file)
            {
                if (file.CompileIndex == lightCompileIndex)
                {
                    ArgumentOutOfRangeException.ThrowIfGreaterThan(localFormId, maximumLightLocalFormId);
                }
                return new FormId<TForm>(dataHandler.LookupFormID(localFormId, fileName));
            }
            return null;
        }

        /// <summary>Gets every form of <typeparamref name="TForm"/>'s form type, as CommonLib's <c>GetFormArray&lt;T&gt;</c> does.</summary>
        /// <remarks>A form that is not a <typeparamref name="TForm"/> is left out. Call on the game's thread.</remarks>
        /// <exception cref="NotSupportedException"><typeparamref name="TForm"/> has no form type of its own, as a base such as TESBoundObject does not.</exception>
        public IReadOnlyList<TForm> GetFormArray<TForm>() where TForm : struct, IForm
        {
            if (PapyrusObjectTypes.Known.TryGetValue(typeof(TForm), out PapyrusObjectType? objectType))
            {
                return [.. dataHandler.GetFormArray(objectType.FormType).Select(FormCasts.CastOrNull<Form, TForm>).WhereNotNull()];
            }
            throw new NotSupportedException($"{typeof(TForm).Name} has no form type of its own, so no form array lists it.");
        }
    }
}

internal static partial class NativeSupplements
{
    [LibraryImport(library)]
    internal static partial int SF_TESDataHandler_GetLoadedMods(nint self, ref nint buffer, int capacity);

    [LibraryImport(library)]
    internal static partial int SF_TESDataHandler_GetLoadedLightMods(nint self, ref nint buffer, int capacity);
}
