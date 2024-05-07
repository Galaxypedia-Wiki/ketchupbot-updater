import fs from "fs/promises";
import { program } from "commander";
import { pathToFileURL } from "url";
import * as Logger from "./Logger.js";

program
    .option("-s, --ships <BOOLEAN>", "Only update ships", false)
    .option("-t, --turrets", "Only update turrets", false);

program.parse();
const OPTIONS = program.opts();

void (async () => {
    console.log((await fs.readFile("banner.txt")).toString());

    if (process.env.NODE_ENV !== "production") {
        Logger.log("Running in development mode", Logger.LogLevel.WARN);
    }

    // Check if the script is being run directly
    if (import.meta.url !== pathToFileURL(process.argv[1]).href) return;
})();
