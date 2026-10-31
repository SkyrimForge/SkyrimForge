using SkyrimForge.Game;
using System;
using System.Globalization;
using System.Text.Json;

namespace SkyrimForge;

/// <summary>Provides extension members for <see cref="ISavedStateEntries"/>.</summary>
public static class SavedStateEntriesExtensions
{
    extension(ISavedStateEntries entries)
    {
        /// <summary>Returns one entry of this store, read and written as <typeparamref name="T"/>.</summary>
        /// <remarks>To store an enum, store its integer, or use <see cref="GetRecordPair{T}"/> with a record that contains it.</remarks>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
        public ISavedStatePair<T> GetPair<T>(string key) where T : struct, IParsable<T>
        {
            ArgumentNullException.ThrowIfNull(key);
            return new SavedStatePair<T>(entries, key);
        }

        /// <summary>Returns one text entry of this store.</summary>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
        public ISavedStateStringPair GetStringPair(string key)
        {
            ArgumentNullException.ThrowIfNull(key);
            return new SavedStateStringPair(entries, key);
        }

        /// <summary>Returns one form ID entry of this store.</summary>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
        public ISavedStateFormIdPair<TForm> GetFormIdPair<TForm>(string key) where TForm : struct, IForm
        {
            ArgumentNullException.ThrowIfNull(key);
            return new SavedStateFormIdPair<TForm>(entries, key);
        }

        /// <summary>Returns one record entry of this store, stored as <see cref="ISavedStateRecordPair{T}"/> describes.</summary>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
        public ISavedStateRecordPair<T> GetRecordPair<T>(string key) where T : class
        {
            ArgumentNullException.ThrowIfNull(key);
            return new SavedStateRecordPair<T>(entries, key);
        }

        /// <summary>Returns a set of text in this store.</summary>
        /// <remarks>A set, list, or dictionary uses the store nested under its key, so two of them under one key read and change each other's entries.</remarks>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
        public ISavedStateStringSet GetStringSet(string key)
        {
            ArgumentNullException.ThrowIfNull(key);
            return new SavedStateStringSet(entries.GetEntries(key));
        }

        /// <summary>Returns a list in this store, whose elements are stores of their own.</summary>
        /// <remarks>
        /// A set, list, or dictionary uses the store nested under its key, so two of them under one key read and change each other's entries.
        /// For a list whose elements are form IDs, use <see cref="GetRecordPair{T}"/>.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
        public ISavedStateList GetList(string key)
        {
            ArgumentNullException.ThrowIfNull(key);
            return new SavedStateList(entries.GetEntries(key));
        }

        /// <summary>Returns a dictionary in this store whose values are stored as text.</summary>
        /// <remarks>
        /// A set, list, or dictionary uses the store nested under its key, so two of them under one key read and change each other's entries.
        /// For a dictionary whose keys are form IDs, use <see cref="GetRecordPair{T}"/>.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
        /// <exception cref="NotSupportedException"><typeparamref name="TKey"/> is neither a built-in integer type, <see cref="byte"/> included, nor an enum.</exception>
        public ISavedStateDictionary<TKey, TValue> GetDictionary<TKey, TValue>(string key)
            where TKey : struct, IConvertible where TValue : struct, IParsable<TValue>
        {
            ArgumentNullException.ThrowIfNull(key);
            return new SavedStateDictionary<TKey, TValue>(SavedStateKey<TKey>.GetDictionaryEntries(entries, key));
        }

        /// <summary>Returns a dictionary in this store whose values are stores of their own.</summary>
        /// <remarks>A set, list, or dictionary uses the store nested under its key, so two of them under one key read and change each other's entries.</remarks>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
        /// <exception cref="NotSupportedException"><typeparamref name="TKey"/> is neither a built-in integer type, <see cref="byte"/> included, nor an enum.</exception>
        public ISavedStateEntriesDictionary<TKey> GetEntriesDictionary<TKey>(string key)
            where TKey : struct, IConvertible
        {
            ArgumentNullException.ThrowIfNull(key);
            return new SavedStateEntriesDictionary<TKey>(SavedStateKey<TKey>.GetDictionaryEntries(entries, key));
        }
    }

    private sealed class SavedStatePair<T>(ISavedStateEntries entries, string key) : ISavedStatePair<T> where T : struct, IParsable<T>
    {
        private readonly ISavedStateEntries entries = entries;

        public string Key { get; } = key;

        public T? Value
        {
            get
            {
                if (entries.GetString(Key) is not { } text)
                {
                    return null;
                }
                return TryParse(text, out T parsedValue) ? parsedValue
                    : throw new FormatException($"The text stored under \"{Key}\" is \"{text}\", which does not parse as {typeof(T).Name}.");
            }
            set => entries.SetString(Key, value != null ? Format(value.Value) : null);
        }

        // The default format drops fractions of a second, so dates and times use the round-trip format.
        // The rest use Convert.ToString, since bool does not implement IFormattable.
        private static string? Format(T value) => value is DateTime or DateTimeOffset or TimeOnly
            ? ((IFormattable)value).ToString("O", CultureInfo.InvariantCulture)
            : Convert.ToString(value, CultureInfo.InvariantCulture);

        // The default style converts a UTC time to local time, so a DateTime is parsed with RoundtripKind.
        private static bool TryParse(string text, out T parsedValue)
        {
            if (typeof(T) == typeof(DateTime))
            {
                bool isDateTime = DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime dateTime);
                parsedValue = (T)(object)dateTime;
                return isDateTime;
            }
            return T.TryParse(text, CultureInfo.InvariantCulture, out parsedValue);
        }
    }

    private sealed class SavedStateStringPair(ISavedStateEntries entries, string key) : ISavedStateStringPair
    {
        private readonly ISavedStateEntries entries = entries;

        public string Key { get; } = key;

        public string? Value
        {
            get => entries.GetString(Key);
            set => entries.SetString(Key, value);
        }
    }

    private sealed class SavedStateRecordPair<T>(ISavedStateEntries entries, string key) : ISavedStateRecordPair<T> where T : class
    {
        private readonly ISavedStateEntries entries = entries;

        public string Key { get; } = key;

        public T? Value
        {
            get
            {
                if (entries.GetString(Key) is not { } json)
                {
                    return null;
                }
                return JsonSerializer.Deserialize<T>(json);
            }
            set => entries.SetString(Key, value != null ? JsonSerializer.Serialize(value) : null);
        }
    }

    private sealed class SavedStateFormIdPair<TForm>(ISavedStateEntries entries, string key) : ISavedStateFormIdPair<TForm> where TForm : struct, IForm
    {
        private readonly ISavedStateEntries entries = entries;

        public string Key { get; } = key;

        public FormId<TForm>? Value
        {
            get => (FormId<TForm>?)entries.GetFormId(Key);
            set => entries.SetFormId(Key, value);
        }
    }
}
