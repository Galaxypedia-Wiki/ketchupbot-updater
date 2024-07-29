using System.Text;
using System.Text.RegularExpressions;

namespace ketchupbot_updater;


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

            // I just want to say to anyone (I guess just me) who is reading this in the future, the reason why we use AsSpan here instead of just Substring is because AsSpan will essentially create a reference to a certain part of the original string, which is more efficient than creating a new string every time we want to get a substring. This saves on memory, so yea.
            // Though, this entire section should probably be flushed out and rewritten to be more efficient. Can probably use something like StringBuilder to make this more efficient.

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
        // Clone the old data to avoid modifying the original dictionary
        var oldDataClone = new Dictionary<string, string>(oldData);
        var updatedParameters = new List<string>();

        foreach (KeyValuePair<string, string> kvp in newData.Where(kvp =>
                     !GlobalConfiguration.ParameterExclusions.Contains(kvp.Key)))
        {
            oldDataClone[kvp.Key] = kvp.Value;
            if (oldDataClone[kvp.Key] != oldData.GetValueOrDefault(kvp.Key)) updatedParameters.Add(kvp.Key);
        }

        Dictionary<string, string> sortedData =
            oldDataClone.OrderBy(kvp => kvp.Key).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        return new Tuple<Dictionary<string, string>, List<string>>(sortedData, updatedParameters);
    }

    /// <summary>
    /// Sanitize data
    /// </summary>
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
                if (oldData.ContainsKey(key))
                {
                    removedParameters.Add(key);
                }

                continue;
            }

            if (key == "description")
            {
                value = value.Replace("\n", " ");
            }

            // Check if the value is a double, and if the decimal is a zero, remove it. Essentially converting 1.0 to 1
            if (double.TryParse(value, out double doubleValue)) value = doubleValue.ToString("G0");

            // Check if the value is a number, and try to add commas to it
            if (int.TryParse(value, out int intValue)) value = intValue.ToString("N0");

            // Remove the title1 parameter
            if (key == "title1") continue;

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
        foreach (KeyValuePair<string,string> keyValuePair in data) sb.AppendLine($"|{keyValuePair.Key} = {keyValuePair.Value}");
        sb.AppendLine("}}");

        return sb.ToString();
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="text"></param>
    /// <param name="infobox"></param>
    /// <returns></returns>
    public static string ReplaceInfobox(string text, string infobox) => SHIP_INFOBOX_REGEX().Replace(text, infobox);

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