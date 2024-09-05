using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace ketchupbot_framework;

/// <summary>
///     Class that contains methods for parsing and manipulating wikitext and ship infobox jsons
/// </summary>
public static partial class WikiParser
{
    private static List<string> SplitTemplate(string text)
    {
        List<string> parts = [];
        string currentPart = "";
        bool inLink = false, inTemplate = false;
        int lastIndex = 0;

        Match match = PairRegex().Match(text);

        while (match.Success)
        {
            string symbol = match.Value;
            int index = match.Index;

            // The reason why we use AsSpan here instead of just Substring is because AsSpan will essentially create a
            // reference to a certain part of the original string, which is more efficient than creating a new string
            // every time we want to get a substring. This saves on memory, so yea. Though, this entire section should
            // probably be flushed out and rewritten to be more efficient. Can probably use something like StringBuilder
            // to make this more efficient.

            switch (symbol)
            {
                case "[[":
                    currentPart += string.Concat(text.AsSpan(lastIndex, index - lastIndex), "[[");
                    inLink = true;
                    break;
                case "]]":
                    currentPart += string.Concat(text.AsSpan(lastIndex, index - lastIndex), "]]");
                    inLink = false;
                    break;
                case "{{":
                    currentPart += string.Concat(text.AsSpan(lastIndex, index - lastIndex), "{{");
                    inTemplate = true;
                    break;
                case "}}":
                    currentPart += string.Concat(text.AsSpan(lastIndex, index - lastIndex), "}}");
                    inTemplate = false;
                    break;
                case "|":
                    if (!inLink && !inTemplate)
                    {
                        parts.Add(string.Concat(currentPart, text.AsSpan(lastIndex, index - lastIndex)));
                        currentPart = "";
                    }
                    else
                    {
                        currentPart += string.Concat(text.AsSpan(lastIndex, index - lastIndex), "|");
                    }

                    break;
            }

            lastIndex = match.Index + match.Length;
            match = match.NextMatch();
        }

        parts.Add(string.Concat(currentPart, text.AsSpan(lastIndex)));
        return parts;
    }

    /// <summary>
    ///     Parses a ship infobox in wikitext into a dictionary of key-value pairs
    /// </summary>
    /// <param name="text">The wikitext input of JUST the template</param>
    /// <returns>A dictionary of key value pairs. The key is the parameter name, and the value is the parameter value</returns>
    /// <exception cref="Exception"></exception>
    public static Dictionary<string, string> ParseInfobox(string text)
    {
        if (text.StartsWith("{{") && text.EndsWith("}}")) text = text.Substring(2, text.Length - 4);

        List<string> infoboxArray = SplitTemplate(text);
        infoboxArray.RemoveAt(0);
        infoboxArray = infoboxArray.Where(part => part.Contains('=')).ToList();

        Dictionary<string, string> infoboxKeyPairs = infoboxArray.Select(part =>
        {
            int splitIndex = part.IndexOf('=');
            string key = part[..splitIndex].Trim();
            string value = part[(splitIndex + 1)..].Trim();
            return new { Key = key, Value = value };
        }).ToDictionary(pair => pair.Key, pair => pair.Value);

        if (!infoboxKeyPairs.TryGetValue("image", out string? image) || !image.StartsWith("<gallery>"))
            return infoboxKeyPairs;

        Match originalGallery = GalleryRegex().Match(text);

        if (!originalGallery.Success)
            throw new InvalidOperationException("Gallery found in infobox but unable to extract it");

        infoboxKeyPairs["image"] = originalGallery.Value;

        return infoboxKeyPairs;
    }

    /// <summary>
    ///     Extracts the infobox from an entire page
    /// </summary>
    /// <param name="text">A page in wikitext</param>
    /// <returns>A string of just the infobox template, in wikitext format</returns>
    /// <exception cref="InvalidOperationException">
    ///     Throws an exception if the infobox cannot be found on the page. This
    ///     likely means that the page lacks an infobox, or has a malformed infobox
    /// </exception>
    public static string ExtractInfobox(string text)
    {
        Match match = SHIP_INFOBOX_REGEX().Match(text);
        if (!match.Success) throw new InvalidOperationException("No infobox found");

        return match.Value;
    }

    /// <summary>
    ///     Merges two dictionaries together, and returns a tuple of the merged dictionary, and a list of updated
    ///     parameters. This preforms a two-way merge, where the new data is merged into the old data.
    /// </summary>
    /// <remarks>
    ///     You should run <see cref="CheckIfInfoboxesChanged" /> prior to running this function to make sure the
    ///     infoboxes are actually different
    /// </remarks>
    /// <param name="newData">The newer data used for updating oldData</param>
    /// <param name="oldData">The oldData that has the preexisting parameters</param>
    /// <returns>
    ///     A tuple. The first item being the merged dictionaries, and the second item being a list of parameters that
    ///     were updated.
    /// </returns>
    /// <exception cref="Exception"></exception>
    public static Tuple<Dictionary<string, string>, List<string>> MergeData(Dictionary<string, string> newData,
        Dictionary<string, string> oldData)
    {
        JObject newDataJObject = JObject.FromObject(newData);
        JObject oldDataJObject = JObject.FromObject(oldData);

        // Remove excluded parameters from the new data
        foreach (string parameter in GlobalConfiguration.ParameterExclusions)
            newDataJObject.Remove(parameter);

        // I wonder, should we refactor this function to return null if both inputs are the same? Or should we leave it as is, having the caller manually run CheckIfInfoboxesChanged?
        oldDataJObject.Merge(newDataJObject, new JsonMergeSettings
        {
            MergeArrayHandling = MergeArrayHandling.Replace,
            MergeNullValueHandling = MergeNullValueHandling.Ignore
        });

        #region Updated Parameter Calculation

        var updatedParameters = new List<string>();

        foreach (KeyValuePair<string, string> kvp in newData)
        {
            // If the key is in the parameter exclusions list, skip it. If the key is not in the old data, or the value is different, add it to the updated parameters list
            // If the key is "title", ignore it
            // If the value is "no" and the key is in the list of parameters to not delete if the value is no, skip it
            // If the value is "yes" and the key is in the list of parameters to delete if the value is yes, skip it
            // TODO: Remove the code duplication here
            if (!GlobalConfiguration.ParameterExclusions.Contains(kvp.Key) &&
                (!oldData.TryGetValue(kvp.Key, out string? oldValue) || oldValue != kvp.Value) &&
                kvp.Key != "title" &&
                !((kvp.Value.Equals("no", StringComparison.OrdinalIgnoreCase) &&
                   !GlobalConfiguration.ParametersToNotDeleteIfValueIsNo.Contains(kvp.Key)) ||
                  (kvp.Value.Equals("yes", StringComparison.OrdinalIgnoreCase) &&
                   GlobalConfiguration.ParametersToDeleteIfValueIsYes.Contains(kvp.Key))))
                updatedParameters.Add(kvp.Key);
        }

        #endregion

        var mergedData = oldDataJObject.ToObject<Dictionary<string, string>>();

        if (mergedData == null) throw new InvalidOperationException("Failed to merge data");

        Dictionary<string, string> sortedData =
            mergedData.OrderBy(kvp => kvp.Key).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        return new Tuple<Dictionary<string, string>, List<string>>(sortedData, updatedParameters);
    }

    /// <summary>
    ///     Sanitize data
    /// </summary>
    /// <remarks>
    ///     Applies the following to the string:
    ///     - Newlines in description are replaced with spaces
    ///     - If the value of the parameter is a double, and the decimal is a zero, then remove it (i.e. 1.0 -> 1)
    ///     - If the value of the parameter is a double, and the decimal is not a zero, then add another zero to it (i.e. 0.2
    ///     -> 0.20)
    ///     - If the key exists in the global ist of parameters to not delete if the value is no, and the value is no, then
    ///     skip it
    ///     - If the key exists in the global list of parameters to delete if the value is yes, and the value is yes, then skip
    ///     it
    ///     - If the value is a number, try to add commas to it
    ///     - Remove the title1 parameter
    /// </remarks>
    /// <param name="data">The data to sanitize</param>
    /// <param name="oldData">The old data pre-merge. Used for tracking removed parameters</param>
    /// <returns>A tuple. The first item being the sanitized data. The second being parameters that were removed in sanitization</returns>
    public static Tuple<Dictionary<string, string>, List<string>> SanitizeData(Dictionary<string, string> data,
        Dictionary<string, string> oldData)
    {
        var sanitizedData = new Dictionary<string, string>();
        var removedParameters = new List<string>();

        foreach ((string? key, string? originalValue) in data)
        {
            if (string.IsNullOrEmpty(originalValue)) continue;
            string value = originalValue.Trim();

            if ((value.Equals("no", StringComparison.OrdinalIgnoreCase) &&
                 !GlobalConfiguration.ParametersToNotDeleteIfValueIsNo.Contains(key)) ||
                (value.Equals("yes", StringComparison.OrdinalIgnoreCase) &&
                 GlobalConfiguration.ParametersToDeleteIfValueIsYes.Contains(key)))
            {
                if (oldData.ContainsKey(key)) removedParameters.Add(key);
                continue;
            }

            // Convert doubles that are non-zero, but have a zero decimal to an integer. So 1.0 becomes 1. But convert 0.2 to 0.20
            if (double.TryParse(value, out double doubleValue))
                value = doubleValue.ToString(doubleValue % 1 == 0 ? "N0" : "N2");

            // Check if the value is a number, and try to add commas to it. But only for integers, not doubles, to not override what we did above. Also don't do it for the title key
            if (int.TryParse(value, out int intValue) && key != "title") value = intValue.ToString("N0");

            switch (key)
            {
                // Remove the title1 parameter
                case "title1":
                    continue;
                // If the title parameter doesn't start with "The ", add it
                case "title" when !value.StartsWith("The ", StringComparison.CurrentCultureIgnoreCase):
                    value = "The " + value;
                    break;
                case "description":
                    // Get rid of newlines in the description parameter
                    value = value.Replace("\n", " ");
                    break;
            }

            sanitizedData[key] = value;
        }

        return new Tuple<Dictionary<string, string>, List<string>>(sanitizedData, removedParameters);
    }

    /// <summary>
    ///     Convert a Dictionary to a wikitext ship infobox
    /// </summary>
    /// <param name="data">The dictionary to convert to wikitext</param>
    /// <returns>A string with the dictionary but in wikitext</returns>
    public static string ObjectToWikitext(Dictionary<string, string> data)
    {
        var sb = new StringBuilder();

        sb.AppendLine("{{Ship Infobox");
        foreach (KeyValuePair<string, string> keyValuePair in data)
            sb.AppendLine($"|{keyValuePair.Key} = {keyValuePair.Value}");
        sb.AppendLine("}}");

        // Replace $ with $$ to escape the $ character
        sb.Replace("$", "$$");

        return sb.ToString().Trim();
    }


    /// <summary>
    ///     Replace the infobox in a page with a new infobox
    /// </summary>
    /// <param name="text">The page</param>
    /// <param name="infobox">The new infobox to replace the old one with</param>
    /// <returns>The new page wikitext with the replaced infobox. Or the original wikitext if the infobox could not be found.</returns>
    public static string ReplaceInfobox(string text, string infobox)
    {
        return SHIP_INFOBOX_REGEX().Replace(text, infobox);
    }

    /// <summary>
    ///     Check two dictionaries to see if they are different
    /// </summary>
    /// <param name="oldData">The old dictionary</param>
    /// <param name="newData">The new dictionary</param>
    /// <returns>Whether they differ or not</returns>
    public static bool
        CheckIfInfoboxesChanged(Dictionary<string, string> oldData, Dictionary<string, string> newData)
    {
        // ReSharper disable once UsageOfDefaultStructEquality
        return !oldData.OrderBy(pair => pair.Key).SequenceEqual(newData.OrderBy(pair => pair.Key));
    }

    /// <summary>
    ///     Extracts turret tables from a page
    /// </summary>
    /// <param name="text">The article to extract from</param>
    /// <returns>A <see cref="MatchCollection" /> with the turret tables</returns>
    /// <exception cref="Exception"></exception>
    public static MatchCollection ExtractTurretTables(string text)
    {
        MatchCollection turretTables = TURRET_TABLE_REGEX().Matches(text);

        return turretTables.Count switch
        {
            0 => throw new InvalidOperationException("No turret tables found."),
            > 6 => throw new InvalidOperationException(
                "Irregular number of turret tables found. Please ensure that the number of tables stays at 6 or below."),
            _ => turretTables
        };
    }

    #region Regexes

    [GeneratedRegex(
        @"{{\s*Ship[ _]Infobox(?:[^{}]|{{[^{}]*}}|{{{[^{}]*}}})+(?:(?!{{(?:[^{}]|{{[^{}]*}}|{{{[^{}]*}}})*)}})",
        RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex SHIP_INFOBOX_REGEX();

    [GeneratedRegex("""{\|\s*class="wikitable sortable".*?\|}""", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex TURRET_TABLE_REGEX();

    [GeneratedRegex(@"\[\[|]]|\{\{|}}|\|")]
    private static partial Regex PairRegex();

    [GeneratedRegex(@"<gallery.*?>.*?</gallery>", RegexOptions.Singleline)]
    private static partial Regex GalleryRegex();

    #endregion

}