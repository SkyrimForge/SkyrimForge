using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace SkyrimForge.Game;

/// <summary>Provides extension members for <see cref="GameSettingCollection"/>.</summary>
public static class GameSettingCollectionExtensions
{
    extension(GameSettingCollection settings)
    {
        /// <summary>Returns the value of the bool game setting named <paramref name="name"/>, such as bDisableAllGore.</summary>
        /// <remarks>Call on the game's thread.</remarks>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <c>null</c>.</exception>
        /// <exception cref="KeyNotFoundException">The game has no setting named <paramref name="name"/>.</exception>
        /// <exception cref="ArgumentException">The setting named <paramref name="name"/> is not of this type.</exception>
        public bool GetBool(string name) => GetTypedSetting(settings, name, Setting.Type.kBool).GetBool();

        /// <summary>Returns the value of the float game setting named <paramref name="name"/>, such as fJumpHeightMin, as Papyrus's Game.GetGameSettingFloat does.</summary>
        /// <inheritdoc cref="GetBool(GameSettingCollection, string)" path="/remarks"/>
        /// <inheritdoc cref="GetBool(GameSettingCollection, string)" path="/exception"/>
        public float GetFloat(string name) => GetTypedSetting(settings, name, Setting.Type.kFloat).GetFloat();

        /// <summary>
        /// Returns the value of the integer game setting named <paramref name="name"/>, such as iTrainingNumAllowedPerLevel, as Papyrus's Game.GetGameSettingInt does.
        /// </summary>
        /// <inheritdoc cref="GetBool(GameSettingCollection, string)" path="/remarks"/>
        /// <inheritdoc cref="GetBool(GameSettingCollection, string)" path="/exception"/>
        public int GetInteger(string name) => GetTypedSetting(settings, name, Setting.Type.kInteger).GetInteger();

        /// <summary>
        /// Returns the value of the string game setting named <paramref name="name"/>, such as sAddItemtoInventory, as Papyrus's Game.GetGameSettingString does.
        /// </summary>
        /// <returns>The text, or <c>null</c> if the setting stores none.</returns>
        /// <inheritdoc cref="GetBool(GameSettingCollection, string)" path="/remarks"/>
        /// <inheritdoc cref="GetBool(GameSettingCollection, string)" path="/exception"/>
        public string? GetString(string name) => GetTypedSetting(settings, name, Setting.Type.kString).GetString();

        /// <summary>
        /// Sets the bool game setting named <paramref name="name"/>, such as bDisableAllGore, to <paramref name="value"/>, as Papyrus's Game.SetGameSettingBool does.
        /// </summary>
        /// <inheritdoc cref="GetBool(GameSettingCollection, string)" path="/remarks"/>
        /// <inheritdoc cref="GetBool(GameSettingCollection, string)" path="/exception"/>
        public void SetBool(string name, bool value) => GetTypedSetting(settings, name, Setting.Type.kBool).SetBool(value);

        /// <summary>
        /// Sets the float game setting named <paramref name="name"/>, such as fJumpHeightMin, to <paramref name="value"/>, as Papyrus's Game.SetGameSettingFloat does.
        /// </summary>
        /// <inheritdoc cref="GetBool(GameSettingCollection, string)" path="/remarks"/>
        /// <inheritdoc cref="GetBool(GameSettingCollection, string)" path="/exception"/>
        public void SetFloat(string name, float value) => GetTypedSetting(settings, name, Setting.Type.kFloat).SetFloat(value);

        /// <summary>
        /// Sets the integer game setting named <paramref name="name"/>, such as iTrainingNumAllowedPerLevel, to <paramref name="value"/>,
        /// as Papyrus's Game.SetGameSettingInt does.
        /// </summary>
        /// <inheritdoc cref="GetBool(GameSettingCollection, string)" path="/remarks"/>
        /// <inheritdoc cref="GetBool(GameSettingCollection, string)" path="/exception"/>
        public void SetInteger(string name, int value) => GetTypedSetting(settings, name, Setting.Type.kInteger).SetInteger(value);

        /// <summary>
        /// Sets the string game setting named <paramref name="name"/>, such as sAddItemtoInventory, to <paramref name="value"/>, as Papyrus's Game.SetGameSettingString does.
        /// </summary>
        /// <remarks>The previous text is not freed. Call on the game's thread.</remarks>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="KeyNotFoundException">The game has no setting named <paramref name="name"/>.</exception>
        /// <exception cref="ArgumentException">The setting named <paramref name="name"/> is not of this type.</exception>
        /// <exception cref="InvalidOperationException">The game's heap could not allocate the text.</exception>
        public void SetString(string name, string value)
        {
            ArgumentNullException.ThrowIfNull(value);
            Setting setting = GetTypedSetting(settings, name, Setting.Type.kString);
            if (NativeSupplements.SF_Setting_SetString(setting.Address, value))
            {
                return;
            }
            throw new InvalidOperationException("The game's heap could not allocate the setting's text.");
        }
    }

    private static Setting GetTypedSetting(GameSettingCollection settings, string name, Setting.Type type)
    {
        ArgumentNullException.ThrowIfNull(name);
        Setting setting = settings.GetSetting(name) ?? throw new KeyNotFoundException($"The game has no setting named {name}.");
        Setting.Type settingType = setting.GetSettingType();
        if (settingType != type)
        {
            throw new ArgumentException($"{name} is a {settingType} setting, not a {type} setting.", nameof(name));
        }
        return setting;
    }
}

internal static partial class NativeSupplements
{
    [LibraryImport(library, StringMarshalling = StringMarshalling.Utf8)]
    [return: MarshalAs(UnmanagedType.U1)]
    internal static partial bool SF_Setting_SetString(nint setting, string value);
}
