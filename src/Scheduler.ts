import cron from "node-cron";
import type APIManager from "./APIManager.js";
import type ShipUpdater from "./ShipUpdater.js";
import type TurretUpdater from "./TurretUpdater.js";

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
                await shipUpdater.updateAll(
                    await apiManager.getShipsData(),
                );
                await turretUpdater.updateTurrets(
                    await apiManager.getTurretData(),
                );
            })();
        }
    }

    startShipScheduler(shipSchedule: string): void {
        if (!cron.validate(shipSchedule))
            throw new Error("Invalid cron schedule");

        this.SHIPSCHEDULERTASK = cron.schedule(shipSchedule, () => {
            void (async () => {
                await this.SHIPUPDATER.updateAll(
                    await this.APIMANAGER.getShipsData(),
                );
            })();
        });
    }

    startTurretScheduler(turretSchedule: string): void {
        if (!cron.validate(turretSchedule))
            throw new Error("Invalid cron schedule");

        this.TURRETSCHEDULERTASK = cron.schedule(turretSchedule, () => {
            void (async () => {
                await this.TURRETUPDATER.updateTurrets(
                    await this.APIMANAGER.getTurretData(),
                );
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
