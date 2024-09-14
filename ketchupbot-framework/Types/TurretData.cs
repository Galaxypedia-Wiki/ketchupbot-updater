using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace ketchupbot_framework.Types;

[Newtonsoft.Json.JsonConverter(typeof(JsonStringEnumConverter))]
public enum TurretTypeEnum
{
    Mining,
    Laser,
    Railgun,
    Flak,
    Cannon,
    Pdl
}

public class TurretData
{
    [JsonProperty] public int Dps { get; set; }

    [JsonProperty] public Dictionary<int, int> Cost { get; set; } = null!;

    [JsonProperty] public int Mass { get; set; }

    [JsonProperty] public string Name { get; set; } = string.Empty;

    [JsonProperty] public string Size { get; set; } = string.Empty;

    [JsonProperty] public string Class { get; set; } = string.Empty;

    [JsonProperty] public string Group { get; set; } = string.Empty;

    [JsonProperty] public int Range { get; set; }

    [JsonProperty] public int Damage { get; set; }

    [JsonProperty] public int Reload { get; set; }

    [JsonProperty] public int BeamSize { get; set; }

    [JsonProperty] public bool Override { get; set; }

    [JsonProperty] public int MaxCycle { get; set; }

    [JsonProperty] public int NumBarrels { get; set; }

    [JsonProperty] public string TurretSize { get; set; } = string.Empty;

    [JsonProperty] public TurretTypeEnum TurretType { get; set; }

    [JsonProperty] public int BaseAccuracy { get; set; }

    [JsonProperty] public int AccuracyIndex { get; set; }

    [JsonProperty] public int RampingStrength { get; set; }

    [JsonProperty] public int SpeedDenominator { get; set; }
}