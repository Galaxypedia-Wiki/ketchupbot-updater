using System.Text.Json.Serialization;
using KBot.Framework.Types;

namespace KBot.Framework.API;

[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata,
    PropertyNameCaseInsensitive = true,
    NumberHandling = JsonNumberHandling.AllowReadingFromString,
    Converters = new[] { typeof(ScalarStringJsonConverter), typeof(JsonStringEnumConverter<TurretTypeEnum>) })]
[JsonSerializable(typeof(Dictionary<string, Dictionary<string, string>>), TypeInfoPropertyName = "ShipData")]
[JsonSerializable(typeof(Dictionary<string, TurretData>), TypeInfoPropertyName = "TurretDictionary")]
internal partial class ApiJsonContext : JsonSerializerContext;
