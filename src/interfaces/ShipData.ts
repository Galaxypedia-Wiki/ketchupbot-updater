import type { SingleShipData } from './SingleShipData.js';

// TODO: Replace ShipData with SingleShipData and reference Record<string, SingleShipData> anywhere ShipData is currently used.
export type ShipData = Record<string, SingleShipData>;