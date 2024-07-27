using System.Diagnostics;
using System.Text.RegularExpressions;
using JsonDiffPatchDotNet;
using ketchupbot_updater.API;
using ketchupbot_updater.Types;
using Newtonsoft.Json;

namespace ketchupbot_updater;

/// <summary>
/// Ship updater class to facilitate updating ship pages. You should pass this class to other classes via dependency injection.
/// </summary>
/// <param name="bot"></param>
/// <param name="apiManager"></param>
public partial class ShipUpdater(MwClient bot, ApiManager apiManager)
{
    private static string GetShipName(string data) => GlobalConfiguration.ShipNameMap.GetValueOrDefault(data, data);
    private static string GetShipName(ShipData data) => GetShipName(data.Title ?? throw new InvalidOperationException("Ship data has no title to use for lookup"));

    /// <summary>
    /// Update a singular ship page with the provided data (or fetch it if not provided)
    /// </summary>
    /// <param name="ship">The name of the ship to update</param>
    /// <param name="data">Supply a <see cref="ShipData"/> object to use for updating. If left null, it will be fetched for you, but this is very bandwidth intensive for mass updating. It is better to grab it beforehand, filter the data for the specific <see cref="ShipData"/> needed, and pass that to the functions.</param>
    public async Task UpdateShip(string ship, ShipData? data = null)
    {
        var updateStart = Stopwatch.StartNew();
        ship = GetShipName(ship);

        #region Data Fetching Logic
        if (data == null)
        {
            Dictionary<string, ShipData>? shipStats = apiManager.GetShipsData().GetAwaiter().GetResult();

            ShipData? shipData = (shipStats ?? throw new InvalidOperationException("Failed to get ship data")).GetValueOrDefault(ship ?? throw new InvalidOperationException("No ship name provided"));

            if (shipData == null)
            {
                Console.WriteLine("Ship not found in API data: " + ship);
                return;
            }

            data = shipData;
        }
        #endregion

        Logger.Log("Updating ship: " + ship);

        #region Article Fetch Logic
        var fetchArticleStart = Stopwatch.StartNew();
        string article = await bot.GetArticle(ship); // Throws exception if article does not exist
        fetchArticleStart.Stop();
        Console.WriteLine($"Fetched article in {fetchArticleStart.ElapsedMilliseconds}ms");
        #endregion

        #region Ignore Flag Check
        if (IGNORE_FLAG_REGEX().IsMatch(article.ToLower())) throw new Exception("Found ignore flag in article");
        #endregion

        // TODO: I think I can probably come up with a better way to do this. The current way of converting the data to a dictionary and then back to a dictionary is a bit silly. I can probably just use the data object directly, and merge the two objects together somehow. This way we don't have to resort to using Dictionary<string, string> for everything and can use the actual data object which is more strongly typed.

        #region Infobox Parsing Logic
        var parsingInfoboxStart = Stopwatch.StartNew();
        Dictionary<string, string> parsedInfobox = WikiParser.ParseInfobox(WikiParser.ExtractInfobox(article));
        parsingInfoboxStart.Stop();
        Console.WriteLine($"Parsed infobox in {parsingInfoboxStart.ElapsedMilliseconds}ms");
        #endregion

        #region Convert data into dictionary
        string dataJson = JsonConvert.SerializeObject(data);
        var dataDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(dataJson);
        #endregion

        #region Data merging logic

#if DEBUG
        var mergeDataStart = Stopwatch.StartNew();
#endif
        Tuple<Dictionary<string, string>, List<string>> mergedData = WikiParser.MergeData(dataDict ?? throw new InvalidOperationException("Supplied data is null after deserialization"), parsedInfobox);
        mergeDataStart.Stop();
        Console.WriteLine($"Merged data in {mergeDataStart.ElapsedMilliseconds}ms");
        #endregion

        #region Data Sanitization Logic
#if DEBUG
        var sanitizeDataStart = Stopwatch.StartNew();
#endif

        Tuple<Dictionary<string, string>, List<string>> sanitizedData = WikiParser.SanitizeData(mergedData.Item1, parsedInfobox);

#if DEBUG
        sanitizeDataStart.Stop();
        Console.WriteLine($"Sanitized data in {sanitizeDataStart.ElapsedMilliseconds}ms");
        Console.WriteLine($"Sanitized data: {JsonConvert.SerializeObject(sanitizedData.Item1, Formatting.Indented)}");
#endif
        #endregion

        #region Diffing logic
        // This logic is only for debugging/development instances to see what changes are being made to the infobox. It is not necessary for the bot to function, so it should not be in production.
#if DEBUG
        var jdp = new JsonDiffPatch();
        string? diff = jdp.Diff(JsonConvert.SerializeObject(sanitizedData.Item1, Formatting.Indented), JsonConvert.SerializeObject(parsedInfobox, Formatting.Indented));

        if (!string.IsNullOrEmpty(diff))
            Console.WriteLine($"Diff:\n{diff}");
#endif
        #endregion

        #region Wikitext Construction Logic
#if DEBUG
        var wikitextConstructionStart = Stopwatch.StartNew();
#endif

        string newWikitext = WikiParser.ReplaceInfobox(article, WikiParser.ObjectToWikitext(sanitizedData.Item1));

#if DEBUG
        wikitextConstructionStart.Stop();
        Console.WriteLine($"Constructed wikitext in {wikitextConstructionStart.ElapsedMilliseconds}ms");
#endif
        #endregion

        #region Article Editing Logic
#if DEBUG
        var articleEditStart = Stopwatch.StartNew();
#endif

        await bot.EditArticle(ship, newWikitext, "Automated ship data update");

#if DEBUG
        articleEditStart.Stop();
        Logger.Log($"Edited article in {articleEditStart.ElapsedMilliseconds}ms");
#endif
        #endregion

#if DEBUG
        updateStart.Stop();
        Logger.Log($"Updated {ship} in {updateStart.ElapsedMilliseconds}ms");
#endif
    }

    [GeneratedRegex(@"<!--\s*ketchupbot-ignore\s*-->", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
    private static partial Regex IGNORE_FLAG_REGEX();
}