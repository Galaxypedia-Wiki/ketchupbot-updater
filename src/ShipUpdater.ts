import type NodeMWP from "./NodeMWP.js";
import * as Logger from "./Logger.js";
import * as WikiParser from "./WikiParser.js";
import * as Diff from "./Diff.js";

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
     * Update an individual ship
     *
     * You will need to catch any errors that are thrown in this function
     * @param ship The ship to update
     * @returns
     */
    public async updateShip(ship: string) {
        const FETCHARTICLESTART = performance.now();
        const ARTICLE: string = (await this.BOT.getArticle(ship)) as string;
        const FETCHARTICLEEND = performance.now();
        Logger.log(
            `Fetching article took ${(FETCHARTICLEEND - FETCHARTICLESTART).toFixed(2)}ms`,
            Logger.LogLevel.DEBUG,
        );

        const INFOBOXPARSESTART = performance.now();
        const PARSEDINFOBOX = WikiParser.parseInfobox(
            WikiParser.extractInfobox(ARTICLE),
        );
        const INFOBOXPARSEEND = performance.now();
        Logger.log(
            `Parsing infobox took ${(INFOBOXPARSEEND - INFOBOXPARSESTART).toFixed(2)}ms`,
            Logger.LogLevel.DEBUG,
        );

        const INFOBOXMERGESTART = performance.now();
        const MERGEDINFOBOX = WikiParser.mergeData(PARSEDINFOBOX, {
            shields: "1000",
        });
        const INFOBOXMERGEEND = performance.now();
        Logger.log(
            `Merging data took ${(INFOBOXMERGEEND - INFOBOXMERGESTART).toFixed(2)}ms`,
            Logger.LogLevel.DEBUG,
        );

        const SANITIZATIONSTART = performance.now();
        const SANITIZEDINFOBOX = WikiParser.sanitizeData(MERGEDINFOBOX);
        const SANITIZATIONEND = performance.now();
        Logger.log(
            `Sanitizing data took ${(SANITIZATIONEND - SANITIZATIONSTART).toFixed(2)}ms`,
            Logger.LogLevel.DEBUG,
        );

        Logger.log(Diff.diffStuff(PARSEDINFOBOX, SANITIZEDINFOBOX));
        return MERGEDINFOBOX;
    }
}
