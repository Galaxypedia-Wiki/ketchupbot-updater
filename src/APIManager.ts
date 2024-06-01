import type { ShipData } from "./interfaces/ShipData.js";
import type { TurretData } from "./interfaces/TurretData.js";
import axios from "axios";
import axiosRetry from "axios-retry";
import * as Logger from "./Logger.js";

// eslint-disable-next-line @typescript-eslint/unbound-method
axiosRetry(axios, { retryDelay: axiosRetry.exponentialDelay });

/**
 * API Manager
 *
 * This class is responsible for managing the API calls to the Galaxy Info, Galaxypedia, and any other APIs
 */
export default class APIManager {
    GALAXY_INFO_API: string;
    GALAXY_INFO_TOKEN: string;
    CACHED_SHIP_DATA: ShipData | null = null;

    constructor(galaxyInfoAPI?: string, galaxyInfoToken?: string) {
        this.GALAXY_INFO_API =
            galaxyInfoAPI ?? process.env.GALAXY_INFO_API ?? "";
        this.GALAXY_INFO_TOKEN =
            galaxyInfoToken ?? process.env.GALAXY_INFO_TOKEN ?? "";

        if (this.GALAXY_INFO_API === "")
            throw new Error("GALAXY_INFO_API is not set");
        if (this.GALAXY_INFO_TOKEN === "")
            throw new Error("GALAXY_INFO_TOKEN is not set");
    }

    /**
     * Get ship data from the Galaxy Info API. 
     * 
     * Will return cached data if the API call fails and cached data is available
     * Complete error handling is not performed here. It is up to the caller to handle errors
     * @param returnCachedOnError Whether to return cached data if the API call fails
     */
    public async getShipsData(returnCachedOnError = true): Promise<ShipData> {
        try {
            const RESPONSE = (
                await axios.get(
                    `${this.GALAXY_INFO_API.trim()}/api/v2/galaxypedia?token=${encodeURIComponent(this.GALAXY_INFO_TOKEN.trim())}`,
                )
            ).data as ShipData;

            this.CACHED_SHIP_DATA = RESPONSE;
            return RESPONSE;
        } catch (error: unknown) {
            if (!axios.isAxiosError(error)) throw error;

            if (returnCachedOnError) {
                Logger.log(
                    `Failed to get ship data: ${error.message}\nAttempting to fall back to cached version`,
                    Logger.LogLevel.WARN,
                );
                if (this.CACHED_SHIP_DATA) return this.CACHED_SHIP_DATA;
                else {
                    Logger.log(
                        "No cached ship data available. Rethrowing error",
                        Logger.LogLevel.ERROR,
                    );
                    throw error;
                }
            } else {
                throw error;
            }
        }
    }

    /**
     * Get a list of ships from the Galaxypedia
     * 
     * Error handling is not performed here
     * @deprecated While not really deprecated, this function should not be used if at all possible.
     */
    public async getGalaxypediaShipList(): Promise<string[]> {
        const RESPONSE = await axios.get(
            "https://robloxgalaxy.wiki/api.php?action=query&format=json&list=categorymembers&cmtitle=Category%3AShips&cmlimit=5000",
        );
        const RESPONSE_DATA = RESPONSE.data as {
            query: {
                categorymembers: {
                    title: string;
                    ns: number;
                    pageid: number;
                }[];
            };
        };

        return RESPONSE_DATA.query.categorymembers
            .map((page) => page.title)
            .filter((pageName) => !pageName.startsWith("Category:"));
    }

    /**
     * Get turret data from the Galaxy Info API
     * 
     * Error handling is not performed here
     */
    public async getTurretData(): Promise<TurretData> {
        const RESPONSE = await axios.get(
            `${this.GALAXY_INFO_API.trim()}/api/v2/ships-turrets/raw`,
        );

        const RESPONSE_DATA = RESPONSE.data as {
            serializedTurrets: TurretData;
        };

        return RESPONSE_DATA.serializedTurrets;
    }
}
