namespace KBot.Framework.Types;

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
    public double Dps { get; set; }

    public Dictionary<int, int> Cost { get; set; } = null!;

    public int Mass { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Size { get; set; } = string.Empty;

    public string Class { get; set; } = string.Empty;

    public string Group { get; set; } = string.Empty;

    public double Range { get; set; }

    public double Damage { get; set; }

    public double Reload { get; set; }

    public double BeamSize { get; set; }

    public bool Override { get; set; }

    public int MaxCycle { get; set; }

    public int NumBarrels { get; set; }

    public string TurretSize { get; set; } = string.Empty;

    public TurretTypeEnum TurretType { get; set; }

    public double BaseAccuracy { get; set; }

    public double AccuracyIndex { get; set; }

    public double RampingStrength { get; set; }

    public double SpeedDenominator { get; set; }
}
