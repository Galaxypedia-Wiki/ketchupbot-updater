import type NodeMWP from "./NodeMWP.js";
import * as WikiParser from "./WikiParser.js";
import type { TurretData } from "./interfaces/TurretData.js";
import APIManager from "./APIManager.js";
import * as Logger from "./Logger.js";

/**
 * Turret Updater
 * 
 * This is the class that is responsible for facilitating the updating of the turret page.
 */
export default class TurretUpdater {
    BOT: NodeMWP;

    constructor(bot: NodeMWP) {
        this.BOT = bot;
    }

    async updateTurrets(turretData?: TurretData) {
        if (!turretData) turretData = await new APIManager().getTurretData();
        const TURRETPAGEWIKITEXT = (await this.BOT.getArticle(
            "Turrets",
        )) as string;
        let new_turret_page_wikitext = TURRETPAGEWIKITEXT;
        const TURRETTABLES = WikiParser.extractTurretTables(TURRETPAGEWIKITEXT);

        for (const [INDEX, TABLE] of TURRETTABLES.entries()) {
            const TABLESPLIT = TABLE.split("|-");

            const RELEVANTTURRETS = Object.entries(turretData).filter(
                ([, data]) => {
                    if (INDEX === 0) return data.TurretType === "Mining";
                    else if (INDEX === 1) return data.TurretType === "Laser";
                    else if (INDEX === 2) return data.TurretType === "Railgun";
                    else if (INDEX === 3) return data.TurretType === "Flak";
                    else if (INDEX === 4) return data.TurretType === "Cannon";
                    else if (INDEX === 5) return data.TurretType === "PDL";
                },
            );

            const TURRETSPARSED = RELEVANTTURRETS.map(([, turret]) => {
                return `\n| ${turret.Name}\n| ${turret.Size}\n| ${turret.BaseAccuracy.toFixed(4)}| ${turret.Damage.toFixed()}\n| ${turret.Range.toFixed()}\n| ${turret.Reload.toFixed(2)}\n| ${turret.SpeedDenominator.toFixed()}\n| ${turret.DPS.toFixed(2)}`;
            });

            const NEWTABLE = `${TABLESPLIT[0].trim()}\n|-\n${TURRETSPARSED.join("\n|-").trim()}\n|}`;

            new_turret_page_wikitext = new_turret_page_wikitext.replace(
                TURRETTABLES[INDEX],
                NEWTABLE,
            );
        }

        if (new_turret_page_wikitext === TURRETPAGEWIKITEXT)
            throw new Error("Turrets page is up to date!");
        
        await this.BOT.edit("Turrets", new_turret_page_wikitext, "Automatic Turret Data Update");
        Logger.log("Updated Turrets page", Logger.LogLevel.INFO, Logger.LogStyle.CHECKMARK);
    }
}