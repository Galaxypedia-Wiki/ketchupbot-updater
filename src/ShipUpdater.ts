import type NodeMWP from "./NodeMWP.js";
import * as Logger from "./Logger.js";
import * as WikiParser from "./WikiParser.js";
import * as Diff from "./Diff.js";
import type { ShipData } from "./interfaces/ShipData.js";
import type { SingleShipData } from "./interfaces/SingleShipData.js";
import APIManager from "./APIManager.js";
import GlobalConfig from "./assets/GlobalConfig.json" with { type: "json" };

/**
 * Ship Updater
 *
 * This is the class that is responsible for facilitating the updating of ship pages.
 */
export default class ShipUpdater {
    BOT: NodeMWP;

    constructor(bot: NodeMWP) {
        this.BOT = bot;
    }

    /**
     * Update all ships with the provided data.
     * 
     * Error handling is performed here
     * @param data The data to update the ships with. If not provided, the data will be fetched from the API
     */
    public async updateAll(data?: ShipData): Promise<void> {
        if (!data) data = await new APIManager().getShipsData();

        for (const SHIP of Object.keys(data)) {
            try {
                await this.updateShip(SHIP, data[SHIP]);
            } catch (error) {
                Logger.log(
                    `Failed to update ship: ${this.getShipName(SHIP)}\n${(error as Error).stack ?? (error as Error).message}`,
                    Logger.LogLevel.ERROR,
                );
            }
        }

        Logger.log(
            "Finished updating all ships",
            Logger.LogLevel.INFO,
            Logger.LogStyle.CHECKMARK,
        );
    }

    /**
     * Get the ship name from the ship data, preforming any necessary name mapping.
     * @param ship The ship to get the name of. Can be a string or a SingleShipData object
     * @private
     */
    private getShipName(ship: SingleShipData | string): string {
        // @ts-expect-error Compiler yells for some reason. We know this is safe (I hope)
        const MAPPED_NAME = GlobalConfig.ship_name_map[
            typeof ship === "string" ? ship : ship.title
        ] as string | undefined;
        return MAPPED_NAME ?? (typeof ship === "string" ? ship : ship.title);
    }

    /**
     * Update an individual ship
     *
     * Error handling is not done here. It is up to the caller to handle errors.
     * @param ship The ship to update
     * @param data The data to update the ship with. If not provided, the data will be fetched from the API
     * @returns
     */
    public async updateShip(
        ship: string,
        data?: SingleShipData,
    ): Promise<void> {
        const UPDATE_START = performance.now();
        //region Data fetching logic
        if (!data) {
            const APIMANAGER = new APIManager();

            const SHIPSTATS: ShipData = await APIMANAGER.getShipsData();
            const SHIPDATA = Object.keys(SHIPSTATS).find(
                (shipData) => shipData === ship,
            );
            if (!SHIPDATA) {
                Logger.log(
                    `Failed to find ship: ${ship}`,
                    Logger.LogLevel.ERROR,
                );
                return;
            }
            data = SHIPSTATS[SHIPDATA];
        }
        //endregion
        ship = this.getShipName(data);
        Logger.log(
            `Updating ship: ${ship}`,
            Logger.LogLevel.INFO,
            Logger.LogStyle.PROGRESS,
        );
        
        //region Article fetch logic
        const FETCH_ARTICLE_START = performance.now();
        const ARTICLE: string = await this.BOT.getArticle(ship) as string;
        if (!ARTICLE) throw new Error(`Failed to fetch article: ${ship}`);
        const FETCH_ARTICLE_END = performance.now();
        Logger.log(
            `Fetching article took ${(FETCH_ARTICLE_END - FETCH_ARTICLE_START).toFixed(2)}ms`,
            Logger.LogLevel.DEBUG,
            Logger.LogStyle.CHECKMARK,
        );
        //endregion

        //region Ignore flag check
        if (
            ARTICLE.toLowerCase().match(/<!--\s*ketchupbot-ignore\s*-->/gim) !==
            null
        )
            throw new Error("Found ignore flag on the page. Skipping update");
        //endregion

        //region Infobox parsing logic
        const INFOBOXPARSESTART = performance.now();
        const PARSED_INFOBOX = WikiParser.parseInfobox(
            WikiParser.extractInfobox(ARTICLE),
        );
        const INFOBOXPARSEEND = performance.now();
        Logger.log(
            `Parsing infobox took ${(INFOBOXPARSEEND - INFOBOXPARSESTART).toFixed(2)}ms`,
            Logger.LogLevel.DEBUG,
            Logger.LogStyle.CHECKMARK,
        );
        //endregion

        //region Convert data into a Partial<Record<string, string>>
        const DATA: Partial<Record<string, string>> = {};
        for (const [KEY, VALUE] of Object.entries(data)) {
            DATA[KEY] = (VALUE as string).toString();
        }
        //endregion

        //region Data merging logic
        const INFOBOX_MERGE_START = performance.now();
        const [MERGED_INFOBOX, UPDATED_PARAMETERS] = WikiParser.mergeData(
            PARSED_INFOBOX,
            DATA,
        );
        const INFOBOX_MERGE_END = performance.now();
        Logger.log(
            `Merging data took ${(INFOBOX_MERGE_END - INFOBOX_MERGE_START).toFixed(2)}ms`,
            Logger.LogLevel.DEBUG,
            Logger.LogStyle.CHECKMARK,
        );
        //endregion

        //region Sanitization logic
        const SANITIZATION_START = performance.now();
        const [SANITIZED_INFOBOX, REMOVED_PARAMETERS] =
            WikiParser.sanitizeData(MERGED_INFOBOX, PARSED_INFOBOX);
        const SANITIZATION_END = performance.now();
        Logger.log(
            `Sanitizing data took ${(SANITIZATION_END - SANITIZATION_START).toFixed(2)}ms`,
            Logger.LogLevel.DEBUG,
            Logger.LogStyle.CHECKMARK,
        );
        //endregion

        //region Diffing logic
        if (JSON.stringify(PARSED_INFOBOX) === JSON.stringify(SANITIZED_INFOBOX)) {
            Logger.log(
                `No changes detected for ship: ${ship}`,
                Logger.LogLevel.INFO,
                Logger.LogStyle.CHECKMARK,
            );
            return;
        }

        if (process.env.NODE_ENV !== "production") console.log(Diff.diffData(PARSED_INFOBOX, SANITIZED_INFOBOX));
        //endregion

        //region Wikitext construction logic
        const WIKITEXT_CONSTRUCTION_START = performance.now();
        const NEW_WIKITEXT = WikiParser.replaceInfobox(
            ARTICLE,
            WikiParser.objectToWikitext(SANITIZED_INFOBOX),
        );
        const WIKITEXT_CONSTRUCTION_END = performance.now();
        Logger.log(
            `Constructing new wikitext took ${(WIKITEXT_CONSTRUCTION_END - WIKITEXT_CONSTRUCTION_START).toFixed(2)}ms`,
            Logger.LogLevel.DEBUG,
            Logger.LogStyle.CHECKMARK,
        );
        //endregion

        //region Article editing logic
        await this.BOT.edit(
            ship,
            NEW_WIKITEXT,
            "Automatic Update" +
                (REMOVED_PARAMETERS.length > 0
                    ? ` | Removed parameters: ${REMOVED_PARAMETERS.sort().join(", ")}`
                    : "") +
                (UPDATED_PARAMETERS.length > 0
                    ? ` | Updated parameters: ${UPDATED_PARAMETERS.filter((param) => !REMOVED_PARAMETERS.includes(param)).sort().join(", ")}`
                    : ""),
        );
        const UPDATEEND = performance.now();
        Logger.log(
            `Updated ship: ${ship} in ${(UPDATEEND - UPDATE_START).toFixed(2)}ms`,
            Logger.LogLevel.INFO,
            Logger.LogStyle.CHECKMARK,
        );
        //endregion
    }
}
