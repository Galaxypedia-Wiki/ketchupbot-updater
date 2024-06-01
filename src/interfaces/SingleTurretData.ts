export interface SingleTurretData {
    DPS: number;
    Cost: Record<number, number>;
    Mass: number;
    Name: string;
    Size: string;
    Class: string;
    Group: string;
    Range: number;
    Damage: number;
    Reload: number;
    BeamSize: number;
    Override: boolean;
    MaxCycles: number;
    NumBarrels: number;
    TurretSize: string;
    TurretType: "Mining" | "Laser" | "Railgun" | "Flak" | "Cannon" | "PDL";
    BaseAccuracy: number;
    AccuracyIndex: number;
    RampingStrength: number;
    SpeedDenominator: number;
}
