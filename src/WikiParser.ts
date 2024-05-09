const SHIP_INFOBOX_REGEX =
    /{{\s*Ship[ _]Infobox(?:[^{}]|{{[^{}]*}}|{{{[^{}]*}}})+(?:(?!{{(?:[^{}]|{{[^{}]*}}|{{{[^{}]*}}})*)}})/is;

import GlobalConfig from "./GlobalConfig.json";

/**
 * Splits the given template into an array of parts. Must not have the {{ or }} at the start and end of the text.
 *
 * @summary Splits the given template into an array of parts.
 * @param text - The text to be split.
 * @returns An array of parts obtained after splitting the text.
 */
export function splitTemplate(text: string): string[] {
    const PARTS: string[] = [];
    let current_part = "";
    let in_link = false;
    let in_template = false;
    let last_index = 0;

    // Use a regular expression to match any of the following: [[, ]], {{, }}, |
    const REGEX = /\[\[|]]|\{\{|}}|\|/g;
    let match;

    while ((match = REGEX.exec(text)) !== null) {
        const [SYMBOL] = match;
        const INDEX = match.index;

        switch (SYMBOL) {
            case "[[":
                current_part += text.slice(last_index, INDEX) + "[";
                in_link = true;
                break;
            case "]]":
                current_part += text.slice(last_index, INDEX) + "]";
                in_link = false;
                break;
            case "{{":
                current_part += text.slice(last_index, INDEX) + "{";
                in_template = true;
                break;
            case "}}":
                current_part += text.slice(last_index, INDEX) + "}";
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
 * Merge two objects together.
 *
 * This function takes newdata and merges it into olddata. If a key in newdata already exists in olddata, the value in olddata will be overwritten. If a key in newdata does not exist in olddata, it will be added.
 * @param oldData
 * @param newData
 */
export function mergeData(
    oldData: Partial<Record<string, string>>,
    newData: Partial<Record<string, string>>,
): Record<string, string> {
    // Make a clone of olddata, so we don't modify the original object
    const OLDDATACLONE: Partial<Record<string, string>> = { ...oldData };

    for (const KEY in newData) {
        if (GlobalConfig.parameter_exclusions.includes(KEY)) continue;

        OLDDATACLONE[KEY] = newData[KEY];
    }

    return Object.entries(OLDDATACLONE)
        .sort(([aKey], [bKey]) => aKey.localeCompare(bKey)) // Sort the entries by key
        .reduce((acc, [key, val]) => ({ ...acc, [key]: val }), {}); // Convert the entries back into an object
}

/**
 * Sanitize data
 * 
 * Creates and returns a new sanitized object
 * @param data
 */
export function sanitizeData(data: Record<string, string>): Record<string, string> {
    const SANITIZED_DATA: Record<string, string> = {};

    for (const [KEY, VALUE] of Object.entries(data)) {
        const NEWVALUE = VALUE.trim();
        if (
            NEWVALUE === "" ||
            (NEWVALUE.toLowerCase() === "no" &&
                // eslint-disable-next-line @typescript-eslint/no-unsafe-call,@typescript-eslint/no-unsafe-member-access
                !GlobalConfig.parameters_to_not_delete_if_value_is_zero.includes(
                    KEY,
                ))
        )
            continue;
        SANITIZED_DATA[KEY] = NEWVALUE;
    }

    return SANITIZED_DATA;
}
