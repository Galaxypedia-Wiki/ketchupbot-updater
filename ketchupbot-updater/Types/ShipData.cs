using Newtonsoft.Json;

namespace ketchupbot_updater.Types;

/// <summary>
/// A record representing a ship's data
/// </summary>
/// NOTE: THIS CLASS DOES NOT INCLUDE SPINAL INFO YET!
public class ShipData
{
    [JsonProperty("title")]
    public string? Title { get; set; }

    [JsonProperty("shields")]
    public string? Shields { get; set; }

    [JsonProperty("hull")]
    public string? Hull { get; set; }

    [JsonProperty("top_speed")]
    public double? TopSpeed { get; set; }

    [JsonProperty("acceleration")]
    public double? Acceleration { get; set; }

    [JsonProperty("turn_speed")]
    public double? TurnSpeed { get; set; }

    [JsonProperty("tiny_turrets")]
    public string? TinyTurrets { get; set; }

    [JsonProperty("small_turrets")]
    public string? SmallTurrets { get; set; }

    [JsonProperty("med_turrets")]
    public string? MedTurrets { get; set; }

    [JsonProperty("large_turrets")]
    public string? LargeTurrets { get; set; }

    [JsonProperty("huge_turrets")]
    public string? HugeTurrets { get; set; }

    [JsonProperty("m_class_range")]
    public string? MClassRange { get; set; }

    [JsonProperty("r_class_range")]
    public string? RClassRange { get; set; }

    [JsonProperty("mining_lasers")]
    public string? MiningLasers { get; set; }

    [JsonProperty("mining_range")]
    public string? MiningRange { get; set; }

    [JsonProperty("fighters")]
    public string? Fighters { get; set; }

    [JsonProperty("cargo_hold")]
    public string? CargoHold { get; set; }

    [JsonProperty("ore_hold")]
    public string? OreHold { get; set; }

    [JsonProperty("warp_drive")]
    public string? WarpDrive { get; set; }

    [JsonProperty("damage_res")]
    public string? DamageRes { get; set; }

    [JsonProperty("stealth")]
    public string? Stealth { get; set; }

    [JsonProperty("cmax_drift")]
    public string? CmaxDrift { get; set; }

    [JsonProperty("turret_dps")]
    public double? TurretDps { get; set; }

    [JsonProperty("spinal_dps")]
    public double? SpinalDps { get; set; }

    [JsonProperty("fighter_turret_dps")]
    public double? FighterTurretDps { get; set; }

    [JsonProperty("fighter_spinal_dps")]
    public double? FighterSpinalDps { get; set; }

    [JsonProperty("antimatter_shard")]
    public double? AntimatterShard { get; set; }

    [JsonProperty("data_archive")]
    public double? DataArchive { get; set; }

    [JsonProperty("ascension_crystal")]
    public double? AscensionCrystal { get; set; }

    [JsonProperty("space_pump")]
    public double? SpacePump { get; set; }

    [JsonProperty("ghost_pumpkin")]
    public double? GhostPumpkin { get; set; }

    [JsonProperty("gamma_pumpkin")]
    public double? GammaPumpkin { get; set; }

    [JsonProperty("void_pumpkin")]
    public double? VoidPumpkin { get; set; }

    [JsonProperty("forgotten_soul")]
    public double? ForgottenSoul { get; set; }

    [JsonProperty("soul")]
    public double? Soul { get; set; }

    [JsonProperty("embryo")]
    public double? Embryo { get; set; }

    [JsonProperty("preos_bit")]
    public double? PreosBit { get; set; }

    [JsonProperty("snowflake")]
    public double? Snowflake { get; set; }

    [JsonProperty("weapons_parts")]
    public double? WeaponsParts { get; set; }

    [JsonProperty("alien_device")]
    public double? AlienDevice { get; set; }

    [JsonProperty("alien_parts")]
    public double? AlienParts { get; set; }

    [JsonProperty("stealth_plating")]
    public double? StealthPlating { get; set; }

    [JsonProperty("plasma_batteries")]
    public double? PlasmaBatteries { get; set; }

    [JsonProperty("thruster_components")]
    public double? ThrustComponents { get; set; }

    [JsonProperty("armored_plating")]
    public double? ArmoredPlating { get; set; }

    [JsonProperty("dimensional_alloy")]
    public double? DimensionalAlloy { get; set; }

    [JsonProperty("quantum_core")]
    public double? QuantumCore { get; set; }

    [JsonProperty("kneall_core")]
    public double? KneallCore { get; set; }

    [JsonProperty("luci_core")]
    public double? LuciCore { get; set; }

    [JsonProperty("permit")]
    public double? Permit { get; set; }

    [JsonProperty("description")]
    public string? Description { get; set; }

    [JsonProperty("vip_required")]
    public string? VipRequired { get; set; }

    [JsonProperty("explosion_radius")]
    public double ExplosionRadius { get; set; }
};