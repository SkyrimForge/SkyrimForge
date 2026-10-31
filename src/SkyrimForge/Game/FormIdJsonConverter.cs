using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SkyrimForge.Game;

// The co-save finds this format in stored JSON as a save is read, and maps each ID through the load order the save recorded.
internal sealed class FormIdJsonConverter : JsonConverterFactory
{
    private const string prefix = "$form:";

    public override bool CanConvert(Type typeToConvert) => typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(FormId<>);

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
        (JsonConverter)Activator.CreateInstance(typeof(Converter<>).MakeGenericType(typeToConvert.GetGenericArguments()))!;

    internal static string Format(uint value) => prefix + value.ToString("X8", CultureInfo.InvariantCulture);

    internal static bool TryParse(string? text, out uint value)
    {
        value = 0;
        return text is { Length: 14 } && text.StartsWith(prefix, StringComparison.Ordinal)
            && uint.TryParse(text.AsSpan(prefix.Length), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out value);
    }

    private static uint Parse(string? text) => TryParse(text, out uint value) ? value : throw new JsonException($"\"{text}\" is not a form ID.");

    // The co-save writes null for a form whose .esm, .esp, or .esl file is no longer loaded. The serializer passes a null to this converter only for the non-nullable type.
    private sealed class Converter<TForm> : JsonConverter<FormId<TForm>> where TForm : struct, IForm
    {
        public override FormId<TForm> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            reader.TokenType == JsonTokenType.Null ? default : new FormId<TForm>(Parse(reader.GetString()));

        public override void Write(Utf8JsonWriter writer, FormId<TForm> value, JsonSerializerOptions options) => writer.WriteStringValue(Format(value.Value));

        public override FormId<TForm> ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            new(Parse(reader.GetString()));

        public override void WriteAsPropertyName(Utf8JsonWriter writer, FormId<TForm> value, JsonSerializerOptions options) => writer.WritePropertyName(Format(value.Value));
    }
}
