using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace ketchupbot_updater;

/// <summary>
/// Class that contains methods for parsing and manipulating wikitext & ship infobox json's
/// </summary>
public static partial class WikiParser
{
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

            // I just want to say to anyone (I guess just me) who is reading this in the future, the reason why we use
            // AsSpan here instead of just Substring is because AsSpan will essentially create a reference to a certain
            // part of the original string, which is more efficient than creating a new string every time we want to get
            // a substring. This saves on memory, so yea. Though, this entire section should probably be flushed out and
            // rewritten to be more efficient. Can probably use something like StringBuilder to make this more
            // efficient.

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
                        currentPart += string.Concat(text.AsSpan(lastIndex, index - lastIndex), "|");

                    break;
            }

            lastIndex = match.Index + match.Length;
            match = match.NextMatch();
        }

        parts.Add(string.Concat(currentPart, text.AsSpan(lastIndex)));
        return parts;
    }

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
            throw new Exception("Gallery found in infobox but unable to extract it");

        infoboxKeyPairs["image"] = originalGallery.Value;

        return infoboxKeyPairs;
    }

    public static string ExtractInfobox(string text)
    {
        Match match = SHIP_INFOBOX_REGEX().Match(text);
        if (!match.Success) throw new Exception("No infobox found");

        return match.Value;
    }

    public static Tuple<Dictionary<string, string>, List<string>> MergeData(Dictionary<string, string> newData,
        Dictionary<string, string> oldData)
    {
        JObject newDataJObject = JObject.FromObject(newData);
        JObject oldDataJObject = JObject.FromObject(oldData);

        // Remove excluded parameters from the new data
        foreach (string parameter in GlobalConfiguration.ParameterExclusions)
            newDataJObject.Remove(parameter);

        oldDataJObject.Merge(newDataJObject, new JsonMergeSettings
        {
            MergeArrayHandling = MergeArrayHandling.Replace,
            MergeNullValueHandling = MergeNullValueHandling.Ignore
        });

        var updatedParameters = new List<string>();

        foreach (KeyValuePair<string, string> kvp in newData)
        {
            // If the key is in the parameter exclusions list, skip it. If the key is not in the old data, or the value is different, add it to the updated parameters list
            if (!GlobalConfiguration.ParameterExclusions.Contains(kvp.Key) &&
                (!oldData.TryGetValue(kvp.Key, out string? oldValue) || oldValue != kvp.Value)) updatedParameters.Add(kvp.Key);
        }

        var mergedData = oldDataJObject.ToObject<Dictionary<string, string>>();

        if (mergedData == null) throw new Exception("Failed to merge data");

        Dictionary<string, string> sortedData =
            mergedData.OrderBy(kvp => kvp.Key).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        return new Tuple<Dictionary<string, string>, List<string>>(sortedData, updatedParameters);
    }

    /// <summary>
    /// Sanitize data
    /// </summary>
    /// <remarks>
    /// Applies the following to the string:
    /// - Newlines in description are replaced with spaces
    /// - If the value of the parameter is a double, and the decimal is a zero, then remove it (i.e. 1.0 -> 1)
    /// - If the value of the parameter is a double, and the decimal is not a zero, then add another zero to it (i.e. 0.2 -> 0.20)
    /// - If the key exists in the global ist of parameters to not delete if the value is no, and the value is no, then skip it
    /// - If the key exists in the global list of parameters to delete if the value is yes, and the value is yes, then skip it
    /// - If the value is a number, try to add commas to it
    /// - Remove the title1 parameter
    /// </remarks>
    /// <param name="data"></param>
    /// <param name="oldData"></param>
    /// <returns></returns>
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
            if (double.TryParse(value, out double doubleValue)) value = doubleValue.ToString(doubleValue % 1 == 0 ? "N0" : "N2");

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
    /// Convert a Dictionary to a wikitext ship infobox
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
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
    ///
    /// </summary>
    /// <param name="text"></param>
    /// <param name="infobox"></param>
    /// <returns></returns>
    public static string ReplaceInfobox(string text, string infobox) => SHIP_INFOBOX_REGEX().Replace(text, infobox);

    public static bool CheckIfInfoboxesChanged(Dictionary<string, string> oldData, Dictionary<string, string> newData)
    {
        return !oldData.OrderBy(pair => pair.Key).SequenceEqual(newData.OrderBy(pair => pair.Key));
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static MatchCollection ExtractTurretTables(string text)
    {
        MatchCollection turretTables = TURRET_TABLE_REGEX().Matches(text);

        return turretTables.Count switch
        {
            0 => throw new Exception("No turret tables found."),
            > 6 => throw new Exception(
                "Irregular number of turret tables found. Please ensure that the number of tables stays at 6 or below."),
            _ => turretTables
        };
    }
}