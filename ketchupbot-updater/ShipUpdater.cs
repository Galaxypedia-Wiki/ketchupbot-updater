using System.Diagnostics;
using System.Text.RegularExpressions;
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

    private const int MaxLength = 12;

    /// <summary>
    /// Mass update all ships with the provided data. If no data is provided, it will fetch the data from the API.
    /// </summary>
    /// <param name="shipDatas">The ship data to use during the update run</param>
    /// <param name="multithreaded">Whether to distribute the ship updates over a ThreadPool, or use a singlethreaded foreach loop</param>
    public async Task UpdateAllShips(Dictionary<string, ShipData>? shipDatas = null, bool multithreaded = true)
    {
        var massUpdateStart = Stopwatch.StartNew();
        shipDatas ??= await apiManager.GetShipsData();
        ArgumentNullException.ThrowIfNull(shipDatas);

        if (multithreaded)
        {
            List<Task> tasks = shipDatas.Select(ship => UpdateShipWrapper(ship.Key, ship.Value)).ToList();

            await Task.WhenAll(tasks);
        }
        else
        {
            foreach (KeyValuePair<string, ShipData> ship in shipDatas)
            {
                await UpdateShipWrapper(ship.Key, ship.Value);
            }
        }

        massUpdateStart.Stop();
        Logger.Log($"Finished updating all ships in {massUpdateStart.ElapsedMilliseconds/1000} seconds", style: LogStyle.Checkmark);
        Logger.Log($"Current thread pool count: {ThreadPool.ThreadCount}");
    }

    /// <summary>
    /// Wrapper for UpdateShip that catches exceptions and logs them instead of throwing them.
    /// </summary>
    /// <param name="ship"></param>
    /// <param name="data"></param>
    private async Task UpdateShipWrapper(string ship, ShipData? data = null)
    {
        try
        {
            await UpdateShip(ship, data);
        }
        catch (Exception e)
        {
            Logger.Log($"{GetShipIdentifier(ship)} Failed to update ship: {e.Message}", level: LogLevel.Error);
        }
    }

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
            Dictionary<string, ShipData>? shipStats = await apiManager.GetShipsData();

            ShipData? shipData = (shipStats ?? throw new InvalidOperationException("Failed to get ship data")).GetValueOrDefault(ship ?? throw new InvalidOperationException("No ship name provided"));

            if (shipData == null)
            {
                Console.WriteLine("Ship not found in API data: " + ship);
                return;
            }

            data = shipData;
        }
        #endregion

        Logger.Log($"{GetShipIdentifier(ship)} Updating ship...", style: LogStyle.Progress);

        #region Article Fetch Logic
#if DEBUG
        var fetchArticleStart = Stopwatch.StartNew();
#endif

        string article = await bot.GetArticle(ship); // Throws exception if article does not exist

#if DEBUG
        fetchArticleStart.Stop();
        Logger.Log($"{GetShipIdentifier(ship)} Fetched article in {fetchArticleStart.ElapsedMilliseconds}ms", style: LogStyle.Checkmark);
#endif
        #endregion

        if (IGNORE_FLAG_REGEX().IsMatch(article.ToLower())) throw new Exception("Found ignore flag in article");

        // I think I can probably come up with a better way to do this. The current way of converting the data to a
        // dictionary and then back to a dictionary is a bit silly. I can probably just use the data object directly,
        // and merge the two objects together somehow. This way we don't have to resort to using Dictionary<string,
        // string> for everything and can use the actual data object which is more strongly typed. The hard part is
        // going to be figuring out how to merge the two objects together. There isn't really a way to enumerate over
        // the properties of an object in C# without using reflection, which is slow. If I went that route, I could
        // probably use reflection to get the properties of the object and then use that to merge the two objects
        // together, but it'll probably have a performance impact. I'll have to think about this a bit more. Also,
        // probably a good idea to read the comment in WikiParser.cs about this

        #region Infobox Parsing Logic
#if DEBUG
        var parsingInfoboxStart = Stopwatch.StartNew();
#endif

        Dictionary<string, string> parsedInfobox = WikiParser.ParseInfobox(WikiParser.ExtractInfobox(article));

#if DEBUG
        parsingInfoboxStart.Stop();
        Logger.Log($"{GetShipIdentifier(ship)} Parsed infobox in {parsingInfoboxStart.ElapsedMilliseconds}ms", style: LogStyle.Checkmark);
#endif
        #endregion

        // Convert data into a dictionary
        var dataDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(data));

        #region Data merging logic
#if DEBUG
        var mergeDataStart = Stopwatch.StartNew();
#endif

        Tuple<Dictionary<string, string>, List<string>> mergedData = WikiParser.MergeData(dataDict ?? throw new InvalidOperationException("Supplied data is null after deserialization"), parsedInfobox);

#if DEBUG
        mergeDataStart.Stop();
        Logger.Log($"{GetShipIdentifier(ship)} Merged data in {mergeDataStart.ElapsedMilliseconds}ms", style: LogStyle.Checkmark);
        // JsonSerializerSettings settings = new()
        // {
        //     NullValueHandling = NullValueHandling.Ignore
        // };
        // Console.WriteLine(JsonConvert.SerializeObject(mergedData.Item1, Formatting.Indented, settings));
#endif
        #endregion

        #region Data Sanitization Logic
#if DEBUG
        var sanitizeDataStart = Stopwatch.StartNew();
#endif

        Tuple<Dictionary<string, string>, List<string>> sanitizedData = WikiParser.SanitizeData(mergedData.Item1, parsedInfobox);

#if DEBUG
        sanitizeDataStart.Stop();
        Logger.Log($"{GetShipIdentifier(ship)} Sanitized data in {sanitizeDataStart.ElapsedMilliseconds}ms", style: LogStyle.Checkmark);
#endif
        #endregion

        #region Diffing logic
        // This logic is only for debugging/development instances to see what changes are being made to the infobox. It is not necessary for the bot to function, so it should not be in production.
        // I've turned it off cuz its kinda annoying
#if DEBUG
        // var jdp = new JsonDiffPatch();
        // string? diff = jdp.Diff(JsonConvert.SerializeObject(sanitizedData.Item1, Formatting.Indented), JsonConvert.SerializeObject(parsedInfobox, Formatting.Indented));

        // if (!string.IsNullOrEmpty(diff))
        //     Console.WriteLine($"Diff:\n{diff}");
#endif
        #endregion

        #region Wikitext Construction Logic
#if DEBUG
        var wikitextConstructionStart = Stopwatch.StartNew();
#endif

        string newWikitext = WikiParser.ReplaceInfobox(article, WikiParser.ObjectToWikitext(sanitizedData.Item1));

#if DEBUG
        wikitextConstructionStart.Stop();
        Logger.Log($"{GetShipIdentifier(ship)} Constructed wikitext in {wikitextConstructionStart.ElapsedMilliseconds}ms", style: LogStyle.Checkmark);
#endif
        #endregion

        #region Article Editing Logic
#if DEBUG
        var articleEditStart = Stopwatch.StartNew();
#endif

        await bot.EditArticle(ship, newWikitext, "Automated ship data update");

#if DEBUG
        articleEditStart.Stop();
        Logger.Log($"{GetShipIdentifier(ship)} Edited page in {articleEditStart.ElapsedMilliseconds}ms", style: LogStyle.Checkmark);
#endif
        #endregion

#if DEBUG
        updateStart.Stop();
        Logger.Log($"{GetShipIdentifier(ship)} Updated ship in {updateStart.ElapsedMilliseconds}ms", style: LogStyle.Checkmark);
        Logger.Log($"Current thread pool count: {ThreadPool.ThreadCount}");
#endif
    }

    /// <summary>
    /// Used to get a ship identifier for logging purposes. I'm pretty sure this only runs in debug mode, so it should be okay to have the overhead from the string formatting.
    /// </summary>
    /// <param name="ship"></param>
    /// <returns></returns>
    private static string GetShipIdentifier(string ship)
    {
        string truncatedShipName = ship.Length > MaxLength ? ship[..MaxLength] : ship;
        string paddedShipName = truncatedShipName.PadRight(MaxLength);

        return $"{paddedShipName} {Environment.CurrentManagedThreadId,-2} |";
    }

    [GeneratedRegex(@"<!--\s*ketchupbot-ignore\s*-->", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
    private static partial Regex IGNORE_FLAG_REGEX();
}