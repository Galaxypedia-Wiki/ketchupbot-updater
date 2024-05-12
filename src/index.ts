import fs from "fs/promises";
import { program } from "commander";
import { pathToFileURL } from "url";
import * as Logger from "./Logger.js";
import NodeMWP from "./NodeMWP.js";
import ShipUpdater from "./ShipUpdater.js";
import APIManager from "./APIManager.js";

program
    .option("-s, --ships", "Only update ships", false)
    .option("-t, --turrets", "Only update turrets", false);

program.parse();
const OPTIONS = program.opts();

void (async () => {
    // Check if the script is being run directly
    if (import.meta.url !== pathToFileURL(process.argv[1]).href) return;

    console.log((await fs.readFile("banner.txt")).toString());

    if (process.env.NODE_ENV !== "production") {
        Logger.log("Running in development mode", Logger.LogLevel.WARN);
        process.env.DRY_RUN = "1";
    }

    const BOT = new NodeMWP({
        protocol: process.env.NODEMW_PROTOCOL ?? "https",
        server: process.env.NODEMW_SERVER ?? "robloxgalaxy.wiki",
        path: process.env.NODEMW_PATH ?? "",
        debug: process.env.NODE_ENV !== "production",
        username: process.env.NODEMW_USERNAME,
        password: process.env.NODEMW_PASSWORD,
    });

    if (process.env.NODEMW_USERNAME && process.env.NODEMW_PASSWORD)
        await BOT.logIn();
    else
        Logger.log(
            "No login credentials provided. NodeMW will now be read-only",
            Logger.LogLevel.WARN,
        );

    const SHIPUPDATER = new ShipUpdater(BOT);
    const APIMANAGER = new APIManager();
    await SHIPUPDATER.updateAll(await APIMANAGER.getShipsData());
})();
