import cron from "node-cron";
import type APIManager from "./APIManager.js";
import type ShipUpdater from "./ShipUpdater.js";
import type TurretUpdater from "./TurretUpdater.js";
import * as Logger from "./Logger.js";

/**
 * Scheduler
 *
 * This class runs the Updater classes at set intervals. Useful for if you don't want to set up scheduling yourself.
 */
export default class Scheduler {
    SHIPSCHEDULERTASK: cron.ScheduledTask | null = null;
    TURRETSCHEDULERTASK: cron.ScheduledTask | null = null;
    APIMANAGER: APIManager;
    SHIPUPDATER: ShipUpdater;
    TURRETUPDATER: TurretUpdater;

    constructor(
        apiManager: APIManager,
        shipUpdater: ShipUpdater,
        turretUpdater: TurretUpdater,
        runOnceAtStart = true
    ) {
        this.APIMANAGER = apiManager;
        this.SHIPUPDATER = shipUpdater;
        this.TURRETUPDATER = turretUpdater;
        
        if (runOnceAtStart) {
            void (async () => {
                Logger.log("Running initial update", Logger.LogLevel.INFO);
                await this.updateShips();
                await this.updateTurrets();
            })();
        }
    }
    
    private async updateShips(): Promise<void> {
        try {
            await this.SHIPUPDATER.updateAll(
                await this.APIMANAGER.getShipsData(),
            );
        } catch (error) {
            Logger.log(
                `Failed to update ships\n${(error as Error).stack ?? (error as Error).message}`,
                Logger.LogLevel.ERROR,
            );
        }
    }
    
    private async updateTurrets(): Promise<void> {
        try {
            await this.TURRETUPDATER.updateTurrets(
                await this.APIMANAGER.getTurretData(),
            );
        } catch (error) {
            Logger.log(
                `Failed to update turrets\n${(error as Error).stack ?? (error as Error).message}`,
                Logger.LogLevel.ERROR,
            );
        }
    }

    startShipScheduler(shipSchedule: string): void {
        if (!cron.validate(shipSchedule))
            throw new Error("Invalid cron schedule");

        this.SHIPSCHEDULERTASK = cron.schedule(shipSchedule, () => {
            void (async () => {
                await this.updateShips();
            })();
        });
    }

    startTurretScheduler(turretSchedule: string): void {
        if (!cron.validate(turretSchedule))
            throw new Error("Invalid cron schedule");

        this.TURRETSCHEDULERTASK = cron.schedule(turretSchedule, () => {
            void (async () => {
                await this.updateTurrets();
            })();
        });
    }

    stopShipScheduler(): void {
        if (this.SHIPSCHEDULERTASK) this.SHIPSCHEDULERTASK.stop();
    }
    
    stopTurretScheduler(): void {
        if (this.TURRETSCHEDULERTASK) this.TURRETSCHEDULERTASK.stop();
    }
}
