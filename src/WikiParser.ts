/**
 * WikiText Parser Utility
 * 
 * This class provides utility functions to parse WikiText.
 */
export default class WikiParser {
    /**
     * Splits the given template into an array of parts. Must not have the {{ or }} at the start and end of the text.
     * 
     * @summary Splits the given template into an array of parts.
     * @param text - The text to be split.
     * @returns An array of parts obtained after splitting the text.
     */
    static splitTemplate(text: string) {
        const PARTS: string[] = [];
        let current_part = "";
        let in_link = false;
        let in_template = false;
        let last_index = 0;

        // Use a regular expression to match the desired patterns
        const REGEX = /\[\[|\]\]|\{\{|\}\}|\|/g;
        let match;

        // Iterate over each match
        while ((match = REGEX.exec(text)) !== null) {
            const [SYMBOL] = match;
            const INDEX = match.index;

            switch (SYMBOL) {
                case '[[':
                    current_part += text.slice(last_index, INDEX) + '[';
                    in_link = true;
                    break;
                case ']]':
                    current_part += text.slice(last_index, INDEX) + ']';
                    in_link = false;
                    break;
                case '{{':
                    current_part += text.slice(last_index, INDEX) + '{';
                    in_template = true;
                    break;
                case '}}':
                    current_part += text.slice(last_index, INDEX) + '}';
                    in_template = false;
                    break;
                case '|':
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
     * Splits the given template into an array of parts.
     *
     * @param text - The text to be parsed.
     * @returns _.
     */
    static parseInfobox(text: string) {

        if (text.startsWith(`{{`) && text.endsWith(`}}`)) {
            text = text.slice(2, -2);
        }

        let infobox_array: string[] = WikiParser.splitTemplate(text);
        infobox_array.shift();

        // Remove part if it doesn't contain an equal sign, leaving only infobox paramters
        infobox_array = infobox_array.filter(function(part: string) {return part.indexOf("=") != -1;} );

        // Split each infobox parameter into key-value pairs
        const INFOBOX_DICT: Record<string, string> = infobox_array.reduce(function(acc: Record<string, string>, part: string){
            const EPARTS = part.split("=");
            acc[EPARTS[0].trim()] = EPARTS.slice(1).join("=").trim();
            return acc;
        },{});

        // Check if the image key's value is a gallery. If so, extract the gallery from the original text and assign it to the image key.
        if (INFOBOX_DICT.image?.startsWith("<gallery")) {
			const ORIGINAL_GALLERY = text.match(/<gallery.*?>.*?<\/gallery>/sg);
			if (ORIGINAL_GALLERY) {
				INFOBOX_DICT.image = ORIGINAL_GALLERY[0];
			}
		}

        return INFOBOX_DICT;
    }
}