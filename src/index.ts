import fs from "fs/promises";
import { program } from "commander";
import { pathToFileURL } from "url";
import * as Logger from "./Logger.js";
import NodeMWP from "./NodeMWP.js";
import ShipUpdater from "./ShipUpdater.js";
import APIManager from "./APIManager.js";
import TurretUpdater from "./TurretUpdater.js";
import { version } from "../package.json";
import Scheduler from "./Scheduler.js";

// eslint-disable-next-line @typescript-eslint/no-unused-vars
function commaSeparatedList(value: string, dummyPrevious: unknown) {
    return value.split(",").map((v) => v.trim());
}

program.version(version);
program
    .option(
        "-s, --ships <string>",
        'List of ships to update. "all" to update all ships, "none" to not update ships',
        commaSeparatedList,
        "all",
    )
    .option("-t, --turrets <boolean>", "Update turrets?", true)
    .option(
        "-mu, --username <string>, --mediawikiusername <string>",
        "Username for logging in to the wiki. This takes precedence over process.env.NODEMW_USERNAME",
    )
    .option(
        "-mp, --password <string>",
        "Password for logging in to the wiki. This takes precedence over process.env.NODEMW_PASSWORD",
    )
    .option(
        "-ga, --galaxy-info-api <string>",
        "Galaxy Info API URL. This takes precedence over process.env.GALAXY_INFO_API",
    )
    .option(
        "-gt, --galaxy-info-token <string>",
        "Galaxy Info API Token. This takes precedence over process.env.GALAXY_INFO_TOKEN",
    )
    .option(
        "-ss, --ship-schedule [string]",
        "Pass to enable ship scheduler. Will ignore ships option.",
        undefined,
    )
    .option(
        "-ts, --turret-schedule [string]",
        "Pass to enable turret scheduler. Will ignore turrets option.",
        undefined,
    )
    .option(
        "--dry-run <boolean>",
        "Run the script in dry-run mode. Defaults to true if running with development configuration",
        undefined,
    );

program.parse();
const OPTIONS = program.opts();
console.log(JSON.stringify(OPTIONS, null, 2));

void (async () => {
    // Check if the script is being run directly
    if (import.meta.url !== pathToFileURL(process.argv[1]).href) return;

    console.log((await fs.readFile("banner.txt")).toString());
    console.log(`ketchupbot-updater | v${version} | ${new Date().toUTCString()}\n`);

    if (process.env.NODE_ENV !== "production") {
        Logger.log("Running in development mode", Logger.LogLevel.WARN);
        if (OPTIONS.dryRun === undefined) OPTIONS.dryRun = true;
    }

    // eslint-disable-next-line @typescript-eslint/no-unsafe-assignment
    if (OPTIONS.dryRun !== undefined)
        process.env.DRY_RUN = OPTIONS.dryRun ? "1" : "0";

    const BOT = new NodeMWP({
        protocol: process.env.NODEMW_PROTOCOL ?? "https",
        server: process.env.NODEMW_SERVER ?? "robloxgalaxy.wiki",
        path: process.env.NODEMW_PATH ?? "",
        debug: process.env.NODE_ENV !== "production",
        username: process.env.NODEMW_USERNAME,
        password: process.env.NODEMW_PASSWORD,
    });

    if (process.env.NODEMW_USERNAME && process.env.NODEMW_PASSWORD) {
        await BOT.logIn();
    } else
        Logger.log(
            "No login credentials provided. NodeMW will now be read-only",
            Logger.LogLevel.WARN,
        );

    const APIMANAGER = new APIManager();
    const SHIPUPDATER = new ShipUpdater(BOT);
    const TURRETUPDATER = new TurretUpdater(BOT);
    let scheduler: Scheduler | null = null;
    
    if (OPTIONS.shipSchedule || OPTIONS.turretSchedule)
        scheduler = new Scheduler(APIMANAGER, SHIPUPDATER, TURRETUPDATER);

    if (OPTIONS.shipSchedule && scheduler)
        // eslint-disable-next-line @typescript-eslint/no-unsafe-argument
        scheduler.startShipScheduler(OPTIONS.shipSchedule);

    if (OPTIONS.turretSchedule && scheduler)
        // eslint-disable-next-line @typescript-eslint/no-unsafe-argument
        scheduler.startTurretScheduler(OPTIONS.turretSchedule);
    
    // Return if we're running in scheduler mode as we don't want to run the update code below
    if (scheduler) return;

    if (
        (typeof OPTIONS.ships === "string" &&
            OPTIONS.ships.toLowerCase() === "all") ||
        // eslint-disable-next-line @typescript-eslint/no-unsafe-member-access,@typescript-eslint/no-unsafe-call
        (Array.isArray(OPTIONS.ships) && OPTIONS.ships[0] === "all")
    )
        await SHIPUPDATER.updateAll(await APIMANAGER.getShipsData());
    else if (
        typeof OPTIONS.ships === "string" &&
        OPTIONS.ships.toLowerCase() === "none"
    ) {
        Logger.log("Skipping ship updates", Logger.LogLevel.INFO);
    } else if (Array.isArray(OPTIONS.ships)) {
        if (OPTIONS.ships.includes("none")) {
            Logger.log("Skipping ship updates", Logger.LogLevel.INFO);
            return;
        }
        for (const SHIP of OPTIONS.ships) {
            try {
                await SHIPUPDATER.updateShip(SHIP as string);
            } catch (error) {
                Logger.log(
                    `Failed to update ship: ${SHIP as string}\n${(error as Error).stack ?? (error as Error).message}`,
                    Logger.LogLevel.ERROR,
                );
            }
        }
    }

    if (OPTIONS.turrets === true) {
        try {
            await TURRETUPDATER.updateTurrets(await APIMANAGER.getTurretData());
        } catch (error) {
            Logger.log(
                `Failed to update turrets\n${(error as Error).stack ?? (error as Error).message}`,
                Logger.LogLevel.ERROR,
            );
        }
    }
})();
