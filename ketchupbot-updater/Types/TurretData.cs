namespace ketchupbot_updater.Types;

public abstract class TurretData
{
    public int Dps { get; set; }
    public Dictionary<int, int> Cost { get; set; } = null!;
    public int Mass { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
    public string Class { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
    public int Range { get; set; }
    public int Damage { get; set; }
    public int Reload { get; set; }
    public int BeamSize { get; set; }
    public bool Override { get; set; }
    public int MaxCycle { get; set; }
    public int NumBarrels { get; set; }
    public string TurretSize { get; set; } = string.Empty;
    public string TurretType { get; set; } = string.Empty; // TODO: Make this into an enum
    public int BaseAccuracy { get; set; }
    public int AccuracyIndex { get; set; }
    public int RampingStrength { get; set; }
    public int SpeedDenominator { get; set; }
}