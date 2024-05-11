/* eslint-disable */
import * as diff from "diff";
import chalk from "chalk";

export function diffData(oldobject: object, newobject: object): string {
    const DIFFERENCE = diff.diffJson(oldobject, newobject);
    let STRINGS: string[] = [];
    for (const PART of DIFFERENCE) {
        STRINGS.push(
            PART.added
                ? chalk.greenBright(PART.value)
                : PART.removed
                  ? chalk.redBright(PART.value)
                  : PART.value,
        );
    }
    
    return STRINGS.join("");
}
