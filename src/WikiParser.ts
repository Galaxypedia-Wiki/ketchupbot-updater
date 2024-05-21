import GlobalConfig from "./GlobalConfig.json" with { type: "json" };

const SHIP_INFOBOX_REGEX =
    /{{\s*Ship[ _]Infobox(?:[^{}]|{{[^{}]*}}|{{{[^{}]*}}})+(?:(?!{{(?:[^{}]|{{[^{}]*}}|{{{[^{}]*}}})*)}})/is;
const TURRET_TABLE_REGEX = /{\|\s*class="wikitable sortable".*?\|}/gis;

import * as Logger from "./Logger.js";

/**
 * Splits the given template into an array of parts. Must not have the {{ or }} at the start and end of the text.
 *
 * @summary Splits the given template into an array of parts.
 * @param text - The text to be split.
 * @returns An array of parts obtained after splitting the text.
 */
function splitTemplate(text: string): string[] {
    const PARTS: string[] = [];
    let current_part = "";
    let in_link = false;
    let in_template = false;
    let last_index = 0;

    // Use a regular expression to match any of the following: [[, ]], {{, }}, |
    const REGEX = /\[\[|]]|\{\{|}}|\|/g;
    let match;

    while ((match = REGEX.exec(text)) !== null) {
        const SYMBOL = match[0];
        const INDEX = match.index;

        switch (SYMBOL) {
            case "[[":
                current_part += text.slice(last_index, INDEX) + "[[";
                in_link = true;
                break;
            case "]]":
                current_part += text.slice(last_index, INDEX) + "]]";
                in_link = false;
                break;
            case "{{":
                current_part += text.slice(last_index, INDEX) + "{{";
                in_template = true;
                break;
            case "}}":
                current_part += text.slice(last_index, INDEX) + "}}";
                in_template = false;
                break;
            case "|":
                if (!in_link && !in_template) {
                    PARTS.push(current_part + text.slice(last_index, INDEX));
                    current_part = "";
                }
                break;
        }

        last_index = REGEX.lastIndex;
    }

    PARTS.push(current_part + text.slice(last_index));
    return PARTS;
}

/**
 * Splits the given infobox and returns it's infobox parameters.
 *
 * @param text - The text to be parsed.
 */
export function parseInfobox(text: string): Partial<Record<string, string>> {
    if (text.startsWith(`{{`) && text.endsWith(`}}`)) text = text.slice(2, -2);

    let infobox_array: string[] = splitTemplate(text);
    infobox_array.shift();
    infobox_array = infobox_array.filter((part: string) => part.includes("="));

    // Split each infobox parameter into key-value pairs
    const INFOBOX_KEYPAIRS: Partial<Record<string, string>> =
        infobox_array.reduce((acc: Record<string, string>, part: string) => {
            const [KEY, ...EPARTS] = part.split("=");
            acc[KEY.trim()] = EPARTS.join("=").trim();
            return acc;
        }, {});

    if (INFOBOX_KEYPAIRS.image?.startsWith("<gallery")) {
        // Grab the gallery from the original text
        const ORIGINAL_GALLERY = text.match(/<gallery.*?>.*?<\/gallery>/gs);
        if (!ORIGINAL_GALLERY)
            throw new Error(
                "Gallery found in infobox but unable to extract it",
            );
        INFOBOX_KEYPAIRS.image = ORIGINAL_GALLERY[0];
    }

    return INFOBOX_KEYPAIRS;
}

export function extractInfobox(text: string): string {
    const MATCH = text.match(SHIP_INFOBOX_REGEX);
    if (!MATCH) throw new Error("No infobox found");

    return MATCH[0];
}

/**
 * Merge two objects together, and return the merged object.
 *
 * This function takes newdata and merges it into olddata. If a key in newdata already exists in olddata, the value in olddata will be overwritten. If a key in newdata does not exist in olddata, it will be added.
 * @param oldData
 * @param newData
 */
export function mergeData(
    oldData: Partial<Record<string, string>>,
    newData: Partial<Record<string, string>>,
): [Record<string, string>, string[]] {
    // Make a clone of olddata, so we don't modify the original object
    const OLDDATACLONE: Partial<Record<string, string>> = { ...oldData };

    const UPDATED_PARAMETERS: string[] = [];

    for (const KEY in newData) {
        if (GlobalConfig.parameter_exclusions.includes(KEY)) continue;

        OLDDATACLONE[KEY] = newData[KEY];
        if (OLDDATACLONE[KEY] !== oldData[KEY]) UPDATED_PARAMETERS.push(KEY);
    }

    return [
        Object.entries(OLDDATACLONE)
            .sort(([aKey], [bKey]) => aKey.localeCompare(bKey)) // Sort the entries by key
            .reduce((acc, [key, val]) => ({ ...acc, [key]: val }), {}), // Convert the entries back into an object
        UPDATED_PARAMETERS,
    ]; 
}

/**
 * Sanitize data
 *
 * Creates and returns a new sanitized object
 * @param data
 */
export function sanitizeData(
    data: Record<string, string>,
): [Record<string, string>, string[]] {
    const SANITIZED_DATA: Record<string, string> = {};
    const REMOVED_PARAMETERS: string[] = [];

    for (const [KEY, VALUE] of Object.entries(data)) {
        const NEWVALUE = VALUE.trim();
        if (
            NEWVALUE === "" ||
            (NEWVALUE.toLowerCase() === "no" &&
                // eslint-disable-next-line @typescript-eslint/no-unsafe-call,@typescript-eslint/no-unsafe-member-access
                !GlobalConfig.parameters_to_not_delete_if_value_is_no.includes(
                    KEY,
                )) ||
            (NEWVALUE.toLowerCase() === "yes" &&
                // eslint-disable-next-line @typescript-eslint/no-unsafe-member-access,@typescript-eslint/no-unsafe-call
                GlobalConfig.parameters_to_delete_if_value_is_yes.includes(KEY))
        ) {
            REMOVED_PARAMETERS.push(KEY);
            continue;
        }
        SANITIZED_DATA[KEY] = NEWVALUE;
    }

    return [SANITIZED_DATA, REMOVED_PARAMETERS];
}

export function objectToWikitext(data: Record<string, string>): string {
    return (
        "{{Ship Infobox\n|" +
        Object.entries(data)
            .map(([key, value]) => `${key} = ${value}`)
            .join("\n|") +
        "\n}}"
    );
}

export function replaceInfobox(text: string, infobox: string): string {
    return text.replace(SHIP_INFOBOX_REGEX, infobox);
}

export function extractTurretTables(text: string): RegExpMatchArray {
    const TURRETTABLES = text.match(TURRET_TABLE_REGEX);
    if (!TURRETTABLES) throw new Error("No turret tables found on the page.");
    if (TURRETTABLES.length > 6)
        throw new Error(
            "Irregular number of tables found on the Turrets page. Please ensure that the number of tables stays at 6 or below.",
        );

    return TURRETTABLES;
}
