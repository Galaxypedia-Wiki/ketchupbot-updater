import type NodeMWP from "./NodeMWP.js";
import * as Logger from "./Logger.js";
import * as WikiParser from "./WikiParser.js";
import * as Diff from "./Diff.js";
import type { ShipData, SingleShipData } from "./interfaces/ShipData.js";
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

    public async updateAll(data?: ShipData): Promise<void> {
        if (!data) data = await new APIManager().getShipsData();

        for (const SHIP of Object.keys(data)) {
            try {
                const UPDATESTART = performance.now();
                await this.updateShip(SHIP, data[SHIP]);
                const UPDATEEND = performance.now();

                Logger.log(
                    `Updated ship: ${this.getShipName(SHIP)} in ${(UPDATEEND - UPDATESTART).toFixed(2)}ms`,
                    Logger.LogLevel.INFO,
                    Logger.LogStyle.CHECKMARK,
                );
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

    private getShipName(ship: SingleShipData | string): string {
        // @ts-expect-error Compiler yells for some reason. We know this is safe (I hope)
        const MAPPEDNAME = GlobalConfig.ship_name_map[
            typeof ship === "string" ? ship : ship.title
        ] as string | undefined;
        return MAPPEDNAME ?? (typeof ship === "string" ? ship : ship.title);
    }

    /**
     * Update an individual ship
     *
     * You will need to catch any errors that are thrown in this function
     * @param ship The ship to update
     * @param data The data to update the ship with. If not provided, the data will be fetched from the API
     * @returns
     */
    public async updateShip(
        ship: string,
        data?: SingleShipData,
    ): Promise<void> {
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
        ship = this.getShipName(data);
        Logger.log(
            `Updating ship: ${ship}`,
            Logger.LogLevel.INFO,
            Logger.LogStyle.PROGRESS,
        );
        const FETCHARTICLESTART = performance.now();
        const ARTICLE: string = (await this.BOT.getArticle(ship)) as string;
        const FETCHARTICLEEND = performance.now();
        Logger.log(
            `Fetching article took ${(FETCHARTICLEEND - FETCHARTICLESTART).toFixed(2)}ms`,
            Logger.LogLevel.DEBUG,
            Logger.LogStyle.CHECKMARK,
        );

        if (!ARTICLE) throw new Error(`Failed to fetch article: ${ship}`);

        if (
            ARTICLE.toLowerCase().match(/<!--\s*ketchupbot-ignore\s*-->/gim) !==
            null
        )
            throw new Error("Found ignore flag on the page. Skipping update");

        const INFOBOXPARSESTART = performance.now();
        const PARSEDINFOBOX = WikiParser.parseInfobox(
            WikiParser.extractInfobox(ARTICLE),
        );
        const INFOBOXPARSEEND = performance.now();
        Logger.log(
            `Parsing infobox took ${(INFOBOXPARSEEND - INFOBOXPARSESTART).toFixed(2)}ms`,
            Logger.LogLevel.DEBUG,
            Logger.LogStyle.CHECKMARK,
        );

        // convert data into a Partial<Record<string, string>>
        const DATA: Partial<Record<string, string>> = {};
        for (const [KEY, VALUE] of Object.entries(data)) {
            DATA[KEY] = (VALUE as string).toString();
        }

        const INFOBOXMERGESTART = performance.now();
        const [MERGEDINFOBOX, UPDATED_PARAMETERS] = WikiParser.mergeData(
            PARSEDINFOBOX,
            DATA,
        );
        const INFOBOXMERGEEND = performance.now();
        Logger.log(
            `Merging data took ${(INFOBOXMERGEEND - INFOBOXMERGESTART).toFixed(2)}ms`,
            Logger.LogLevel.DEBUG,
            Logger.LogStyle.CHECKMARK,
        );

        const SANITIZATIONSTART = performance.now();
        const [SANITIZEDINFOBOX, REMOVEDPARAMETERS] =
            WikiParser.sanitizeData(MERGEDINFOBOX);
        const SANITIZATIONEND = performance.now();
        Logger.log(
            `Sanitizing data took ${(SANITIZATIONEND - SANITIZATIONSTART).toFixed(2)}ms`,
            Logger.LogLevel.DEBUG,
            Logger.LogStyle.CHECKMARK,
        );

        const WIKITEXTCONSTRUCTIONSTART = performance.now();
        const NEWWIKITEXT = WikiParser.replaceInfobox(
            ARTICLE,
            WikiParser.objectToWikitext(SANITIZEDINFOBOX),
        );
        const WIKITEXTCONSTRUCTIONEND = performance.now();

        Logger.log(
            `Constructing new wikitext took ${(WIKITEXTCONSTRUCTIONEND - WIKITEXTCONSTRUCTIONSTART).toFixed(2)}ms`,
            Logger.LogLevel.DEBUG,
            Logger.LogStyle.CHECKMARK,
        );

        if (ARTICLE === NEWWIKITEXT) {
            Logger.log(
                `No changes detected for ship: ${ship}`,
                Logger.LogLevel.INFO,
                Logger.LogStyle.CHECKMARK,
            );
            return;
        }

        console.log(Diff.diffData(PARSEDINFOBOX, SANITIZEDINFOBOX));

        try {
            await this.BOT.edit(
                ship,
                NEWWIKITEXT,
                "Automatic Update" +
                    (REMOVEDPARAMETERS.length > 0
                        ? ` | Removed parameters: ${REMOVEDPARAMETERS.join(", ")}`
                        : "") +
                    (UPDATED_PARAMETERS.length > 0
                        ? ` | Updated parameters: ${UPDATED_PARAMETERS.join(", ")}`
                        : ""),
            );
        } catch (error) {
            Logger.log(
                `Failed to edit ${ship}:\n${(error as Error).stack ?? (error as Error).message}`,
                Logger.LogLevel.ERROR,
            );
        }
    }
}
