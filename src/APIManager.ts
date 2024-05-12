import type { ShipData } from "./interfaces/ShipData.js";
import type TurretData from "./interfaces/TurretData.js";
import axios from "axios";
import axiosRetry from "axios-retry";

// eslint-disable-next-line @typescript-eslint/unbound-method
axiosRetry(axios, { retryDelay: axiosRetry.exponentialDelay });

/**
 * API Manager
 *
 * This class is responsible for managing the API calls to the Galaxy Info and Galaxypedia API
 */
export default class APIManager {
    GALAXY_INFO_API: string;
    GALAXY_INFO_TOKEN: string;

    constructor(galaxyInfoAPI?: string, galaxyInfoToken?: string) {
        this.GALAXY_INFO_API =
            galaxyInfoAPI ?? process.env.GALAXY_INFO_API ?? "";
        this.GALAXY_INFO_TOKEN =
            galaxyInfoToken ?? process.env.GALAXY_INFO_TOKEN ?? "";

        if (this.GALAXY_INFO_API === "") {
            throw new Error("GALAXY_INFO_API is not set");
        } else if (this.GALAXY_INFO_TOKEN === "") {
            throw new Error("GALAXY_INFO_TOKEN is not set");
        }
    }

    public async getShipsData(): Promise<ShipData> {
        const RESPONSE = await axios.get(
            `${this.GALAXY_INFO_API.trim()}/api/v2/galaxypedia?token=${this.GALAXY_INFO_TOKEN.trim()}`,
        );
        console.log(
            `${this.GALAXY_INFO_API.trim()}/api/v2/galaxypedia?token=${this.GALAXY_INFO_TOKEN.trim()}`,
        );

        return RESPONSE.data as ShipData;
    }

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

    public async getTurretData(): Promise<TurretData[]> {
        const RESPONSE = await axios.get(
            `${this.GALAXY_INFO_API.trim()}/api/v2/ships-turrets/raw`,
        );

        const RESPONSE_DATA = RESPONSE.data as {
            serializedTurrets: TurretData[];
        };

        return RESPONSE_DATA.serializedTurrets;
    }
}
