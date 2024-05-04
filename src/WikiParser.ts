/**
 * WikiText Parser Utility
 * 
 * This class provides utility functions to parse WikiText.
 */
export default class WikiParser {
    /**
     * Splits the given template into an array of parts.
     *
     * @param text - The text to be split.
     * @returns An array of parts obtained after splitting the text.
     */
    static splitTemplate(text: string) {
        const PARTS: string[] = [];
        let current_part = "";
        let in_link = false;
        let in_template = false;

        // Iterate over each character in the text, and split it into parts based on the "|" character, and also handle the "[" and "{" characters.
        for (let char_num = 0; char_num < text.length; char_num++) {
            if (text[char_num] == "[" && text[char_num + 1] == "[") {
                current_part += "[";
                in_link = true;
                char_num++;
            } else if (text[char_num] == "]" && text[char_num + 1] == "]") {
                current_part += "]";
                in_link = false;
                char_num++;
            }
            if (text[char_num] == "{" && text[char_num + 1] == "{") {
                current_part += "{";
                in_template = true;
                char_num++;
            } else if (text[char_num] == "}" && text[char_num + 1] == "}") {
                current_part += "}";
                in_template = false;
                char_num++;
            }
            if (text[char_num] == "|" && !in_link && !in_template) {
                PARTS.push(current_part);
                current_part = "";
            } else {
                current_part += text[char_num];
            }
        }
        PARTS.push(current_part);
        return PARTS;
    }

    /**
     * Splits the given template into an array of parts.
     *
     * @param text - The text to be parsed.
     * @returns _.
     */
    static parseInfobox(text: string) {
        const INFOBOX_PARTS: string[] = WikiParser.splitTemplate(text);
        INFOBOX_PARTS.shift();
    }
}
