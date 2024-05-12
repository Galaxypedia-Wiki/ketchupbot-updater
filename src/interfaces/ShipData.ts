
export type ShipData = Record<string, SingleShipData>;

/**
 * Single Ship Data
 * 
 * Note that there may be fields titled "spinal_*" which can't be typed as they are dynamically generated
 */
export interface SingleShipData {
    title: string;
    shields: string;
    hull: string;
    top_speed: number;
    acceleration: number;
    turn_speed: number;
    tiny_turrets?: string;
    small_turrets?: string;
    med_turrets?: string;
    large_turrets?: string;
    huge_turrets?: string;
    m_class_range?: string;
    r_class_range?: string;
    mining_lasers?: string;
    mining_range?: string;
    fighters?: string;
    cargo_hold?: number;
    ore_hold?: number;
    warp_drive: string;
    damage_res: string;
    stealth: string;
    cmax_drift?: string;
    turret_dps?: number;
    spinal_dps?: number;
    fighter_turret_dps?: number;
    fighter_spinal_dps?: number;
    antimatter_shard?: number;
    data_archive?: number;
    ascension_crystal?: number;
    space_pump?: number;
    ghost_pumpkin?: number;
    gamma_pumpkin?: number;
    void_pumpkin?: number;
    forgotten_soul?: number;
    soul?: number;
    embryo?: number;
    preos_bit?: number;
    snowflake?: number;
    weapons_parts?: number;
    alien_device?: number;
    alien_parts?: number;
    stealth_plating?: number;
    plasma_batteries?: number;
    thrust_components?: number;
    armored_plating?: number;
    dimensional_alloy?: number;
    quantum_core?: number;
    kneall_core?: number;
    luci_core?: number;
    permit?: number;
    description: string;
    vip_required: string;
    explosion_radius: number;
}
