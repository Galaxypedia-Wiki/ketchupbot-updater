import chalk from "chalk";
import fs from "fs/promises";
import { program } from "commander";

program
    .option("-s, --ships <BOOLEAN>", "Only update ships", false)
    .option("-t, --turrets", "Only update turrets", false);

program.parse();
const OPTIONS = program.opts();

void (async () => {
    console.log((await fs.readFile("banner.txt")).toString());

    if (process.env.NODE_ENV !== "production") {
        console.log(
            `${chalk.red("[!]")} Running in development mode! Dry run enabled.`
        );
    }

    if (require.main !== module) return;
})();
