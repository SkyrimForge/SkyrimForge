using System;
using System.Globalization;

namespace SkyrimForge;

internal static class SavedStateKey<TKey> where TKey : struct, IConvertible
{
    private static readonly TypeCode typeCode = Type.GetTypeCode(typeof(TKey));
    private static readonly bool isEnum = typeof(TKey).IsEnum;

    internal static ISavedStateEntries GetDictionaryEntries(ISavedStateEntries entries, string key)
    {
        if (typeCode is >= TypeCode.SByte and <= TypeCode.UInt64)
        {
            return entries.GetEntries(key);
        }
        throw new NotSupportedException($"{typeof(TKey).Name} is neither a built-in integer type, byte included, nor an enum, so a saved-state dictionary cannot store it as a key.");
    }

    internal static string Format(TKey key) => string.Create(CultureInfo.InvariantCulture, $"{Convert.ChangeType(key, typeCode, CultureInfo.InvariantCulture)}");

    // Reformatted and compared, so text that parses but is not canonical, such as " 6" or "+6", is rejected.
    internal static bool TryParse(string text, out TKey key)
    {
        key = default;
        object? number;
        try
        {
            number = Convert.ChangeType(text, typeCode, CultureInfo.InvariantCulture);
        }
        catch (Exception ex) when (ex is FormatException or OverflowException)
        {
            return false;
        }
        if (number != null)
        {
            key = (TKey)(isEnum ? Enum.ToObject(typeof(TKey), number) : number);
            return Format(key) == text;
        }
        return false;
    }
}
