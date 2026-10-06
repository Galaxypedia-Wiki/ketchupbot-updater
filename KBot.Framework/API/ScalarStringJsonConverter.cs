using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KBot.Framework.API;

// Preserve the API's scalar-to-string behavior without reflection-based serialization.
internal sealed class ScalarStringJsonConverter : JsonConverter<string>
{
    public override string ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.GetString()!;

    public override void WriteAsPropertyName(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        => writer.WritePropertyName(value);

    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.String => reader.GetString(),
            JsonTokenType.True => "true",
            JsonTokenType.False => "false",
            JsonTokenType.Number => reader.TryGetInt64(out long integer)
                ? integer.ToString(CultureInfo.InvariantCulture)
                : reader.GetDouble().ToString("R", CultureInfo.InvariantCulture),
            JsonTokenType.Null => null,
            _ => throw new JsonException("Expected a scalar string value")
        };
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value);
    }
}
