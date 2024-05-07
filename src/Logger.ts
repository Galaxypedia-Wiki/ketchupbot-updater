import chalk from "chalk";
import axios from "axios";

/**
 * Log Levels
 * 
 * DEBUG: Debugging/verbose messages that should only be logged in development  
 * INFO: Informational messages that should be logged in production  
 * WARN: Warning messages  
 * ERROR: Error messages  
 */
export enum LogLevel {
    DEBUG,
    INFO,
    WARN,
    ERROR
}

/**
 * Log Styles
 * 
 * Typically an appropriate style is chosen based on the log level so you don't need to specify this
 */
export enum LogStyle {
    NORMAL = "[-] ",
    CHECKMARK = "[✔] ",
    PROGRESS = "[>] ",
    HANG = "[~] ",
    WARNING = "[!] ",
}

/**
 * Log a message to the console
 * @param message The message to log
 * @param level The log level. Defaults to LogLevel.INFO
 * @param style The log style. Defaults to an appropriate style based on the log level
 */
export function log(message: string, level: LogLevel = LogLevel.INFO, style?: LogStyle): void {
    if (!style) {
        switch (level) {
            case LogLevel.ERROR:
            case LogLevel.WARN:
                style = LogStyle.WARNING;
                break;
            default:
                style = LogStyle.NORMAL;
                break;
        }
    }

    switch (level) {
        case LogLevel.DEBUG:
            if (process.env.NODE_ENV !== "production") console.log(style + message);
            break;
        case LogLevel.INFO:
            console.log(style + message);
            break;
        case LogLevel.WARN:
            console.warn(chalk.yellowBright(style + message));
            break;
        case LogLevel.ERROR:
            console.error(chalk.redBright(style + message));
            break;
    }
}

export async function logToDiscord(message: string): Promise<void> {
    if (!process.env.DISCORD_WEBHOOK) throw new Error("No Discord webhook URL provided!");

    await axios.post(process.env.DISCORD_WEBHOOK, {
        content: message
    });

    log("Logged message to Discord", LogLevel.DEBUG);
}