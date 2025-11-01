using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
namespace ketchupbot_framework.Types;

[Newtonsoft.Json.JsonConverter(typeof(StringEnumConverter))]
public enum TurretTypeEnum
{
    Mining,
    Laser,
    Railgun,
    Flak,
    Cannon,
    Pdl,
    Beam
}

public class TurretData
{
    [JsonProperty] public double? Dps { get; set; }

    [JsonProperty] public Dictionary<int, int> Cost { get; set; } = null!;

    [JsonProperty] public double? Mass { get; set; }

    [JsonProperty] public string? Name { get; set; } = string.Empty;

    [JsonProperty] public string? Size { get; set; } = string.Empty;

    [JsonProperty] public string? Class { get; set; } = string.Empty;

    [JsonProperty] public string? Group { get; set; } = string.Empty;

    [JsonProperty] public double? Range { get; set; }

    [JsonProperty] public double? Damage { get; set; }

    [JsonProperty] public double? Reload { get; set; }

    [JsonProperty] public double? BeamSize { get; set; }

    [JsonProperty] public bool? Override { get; set; }

    [JsonProperty] public int? MaxCycle { get; set; }

    [JsonProperty] public int? NumBarrels { get; set; }

    [JsonProperty] public string? TurretSize { get; set; } = string.Empty;

    [JsonProperty] public TurretTypeEnum TurretType { get; set; }

    [JsonProperty] public double? BaseAccuracy { get; set; }

    [JsonProperty] public double? AccuracyIndex { get; set; }

    [JsonProperty] public double? RampingStrength { get; set; }

    [JsonProperty] public double? SpeedDenominator { get; set; }
}