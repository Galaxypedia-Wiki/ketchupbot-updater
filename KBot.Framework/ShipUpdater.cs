using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using KBot.Framework.API;
using Microsoft.Extensions.Logging;

namespace KBot.Framework;

/// <summary>
///     Ship updater class to facilitate updating ship pages. You should pass this class to other classes via dependency
///     injection.
/// </summary>
/// <param name="bot">The <see cref="MediaWikiClient" /> instance to use for interacting with the wiki</param>
/// <param name="apiManager">The <see cref="ApiManager" /> instance to use for making API requests</param>
/// <param name="logger">The logger for ship updates</param>
public partial class ShipUpdater(MediaWikiClient bot, ApiManager apiManager, ILogger<ShipUpdater> logger, bool dryRun = false, bool verbose = false)
{
    private const int MaxLength = 12;

    private static string GetShipName(string data)
    {
        return GlobalConfiguration.ShipNameMap.GetValueOrDefault(data, data);
    }

    /// <summary>
    ///     Mass update all ships with the provided data. If no data is provided, it will fetch the data from the API.
    /// </summary>
    /// <param name="shipDatas">The ship data to use during the update run</param>
    /// <param name="threads"></param>
    public async Task UpdateAllShips(Dictionary<string, Dictionary<string, string>>? shipDatas = null,
        int threads = -1, Action<ShipUpdateProgress>? progress = null)
    {
        await MassUpdateShips((await apiManager.GetShipsData()).Keys.ToList(), shipDatas, threads, progress);
    }

    /// <summary>
    ///     Update multiple ships using the provided data.
    /// </summary>
    /// <param name="ships">A list of ships to update</param>
    /// <param name="shipDatas"></param>
    /// <param name="threads"></param>
    public async Task MassUpdateShips(List<string> ships,
        Dictionary<string, Dictionary<string, string>>? shipDatas = null, int threads = -1, Action<ShipUpdateProgress>? progress = null)
    {
        var massUpdateStart = Stopwatch.StartNew();
        shipDatas ??= await apiManager.GetShipsData();
        ArgumentNullException.ThrowIfNull(shipDatas);

        Dictionary<string, string> articles = await bot.GetArticlesAsync(ships.ToArray());

        int updated = 0, unchanged = 0, failed = 0;
        object progressLock = new();
        progress?.Invoke(new(ships.Count, updated, unchanged, failed));

        await Parallel.ForEachAsync(ships, new ParallelOptions
        {
            MaxDegreeOfParallelism = threads
        }, async (ship, _) =>
        {
            ShipUpdateOutcome outcome;
            try
            {
                logger.LogDebug("{Identifier} Updating ship...", GetShipIdentifier(ship));
                await UpdateShip(ship, shipDatas.GetValueOrDefault(ship), articles.GetValueOrDefault(ship));
                outcome = ShipUpdateOutcome.Updated;
                logger.LogDebug("{ShipIdentifier} Updated ship", GetShipIdentifier(ship));
            }
            catch (ShipAlreadyUpdatedException)
            {
                outcome = ShipUpdateOutcome.Unchanged;
                logger.LogDebug("{Identifier} Ship is up-to-date", GetShipIdentifier(ship));
            }
            catch (Exception e)
            {
                outcome = ShipUpdateOutcome.Failed;
                if (verbose) logger.LogError(e, "{Identifier} Failed to update ship", GetShipIdentifier(ship));
            }

            lock (progressLock)
            {
                switch (outcome)
                {
                    case ShipUpdateOutcome.Updated: updated++; break;
                    case ShipUpdateOutcome.Unchanged: unchanged++; break;
                    case ShipUpdateOutcome.Failed: failed++; break;
                }
                progress?.Invoke(new(ships.Count, updated, unchanged, failed));
            }
        });

        massUpdateStart.Stop();
        logger.LogDebug("Finished updating ships in {Elapsed}s", massUpdateStart.ElapsedMilliseconds / 1000);
    }

    /// <summary>
    ///     Update a singular ship page with the provided data (or fetch it if not provided)
    /// </summary>
    /// <param name="ship">The name of the ship to update</param>
    /// <param name="data">
    ///     Supply a <see cref="Dictionary{TKey,TValue}" /> to use for updating. If left null, it will be
    ///     fetched for you, but this is very bandwidth intensive for mass updating. It is better to grab it beforehand, filter
    ///     the data for the specific <see cref="Dictionary{TKey,TValue}" /> needed, and pass that to the functions.
    /// </param>
    /// <param name="shipArticle">
    ///     Provide a string to use as an article. If left null, it will be fetched based on
    ///     <paramref name="ship" />
    /// </param>
    private async Task UpdateShip(string ship, Dictionary<string, string>? data = null, string? shipArticle = null)
    {
        ship = GetShipName(ship);

        #region Data Fetching Logic

        if (data == null)
        {
            Dictionary<string, Dictionary<string, string>>? shipStats = await apiManager.GetShipsData();

            Dictionary<string, string>? shipData =
                (shipStats ?? throw new InvalidOperationException("Failed to get ship data")).GetValueOrDefault(
                    ship ?? throw new InvalidOperationException("No ship name provided"));

            if (shipData == null)
            {
                throw new InvalidOperationException($"Ship not found in API data: {ship}");
            }

            data = shipData;
        }

        #endregion

        #region Article Fetch Logic

        if (shipArticle == null)
        {
#if DEBUG
            var fetchArticleStart = Stopwatch.StartNew();
#endif

            shipArticle = await bot.GetArticleAsync(ship); // Throws exception if article does not exist

#if DEBUG
            fetchArticleStart.Stop();
            logger.LogDebug("{Identifier} Fetched article in {1}ms", GetShipIdentifier(ship),
                fetchArticleStart.ElapsedMilliseconds);
#endif
        }

        #endregion

        if (IGNORE_FLAG_REGEX().IsMatch(shipArticle.ToLower()))
            throw new InvalidOperationException("Found ignore flag in article");

        #region Infobox Parsing Logic

#if DEBUG
        var parsingInfoboxStart = Stopwatch.StartNew();
#endif

        Dictionary<string, string> parsedInfobox = WikiParser.ParseInfobox(WikiParser.ExtractInfobox(shipArticle));

#if DEBUG
        parsingInfoboxStart.Stop();
        logger.LogDebug("{Identifier} Parsed infobox in {1}ms", GetShipIdentifier(ship),
            parsingInfoboxStart.ElapsedMilliseconds);
#endif

        #endregion

        #region Data merging logic

#if DEBUG
        var mergeDataStart = Stopwatch.StartNew();
#endif

        (Dictionary<string, string> sortedData, List<string> updatedParameters) mergedData = WikiParser.MergeData(
            data ?? throw new InvalidOperationException("Supplied data is null after deserialization"), parsedInfobox);

#if DEBUG
        mergeDataStart.Stop();
        logger.LogDebug("{Identifier} Merged data in {1}ms", GetShipIdentifier(ship), mergeDataStart.ElapsedMilliseconds);
#endif

        #endregion

        #region Data Sanitization Logic

#if DEBUG
        var sanitizeDataStart = Stopwatch.StartNew();
#endif

        (Dictionary<string, string> finalData, List<string> updatedParameters) sanitizedData = WikiParser.SanitizeData(mergedData.sortedData, parsedInfobox);

#if DEBUG
        sanitizeDataStart.Stop();
        logger.LogDebug("{Identifier} Sanitized data in {1}ms", GetShipIdentifier(ship),
            sanitizeDataStart.ElapsedMilliseconds);
#endif

        #endregion

        #region Diffing logic

        if (!WikiParser.CheckIfInfoboxesChanged(sanitizedData.finalData, parsedInfobox))
            throw new ShipAlreadyUpdatedException("No changes detected");

        #endregion

        #region Wikitext Construction Logic

#if DEBUG
        var wikitextConstructionStart = Stopwatch.StartNew();
#endif

        string newWikitext = WikiParser.ReplaceInfobox(shipArticle, WikiParser.ObjectToWikitext(sanitizedData.finalData));

#if DEBUG
        wikitextConstructionStart.Stop();
        logger.LogDebug("{Identifier} Constructed wikitext in {1}ms", GetShipIdentifier(ship),
            wikitextConstructionStart.ElapsedMilliseconds);
#endif

        #endregion

        #region Article Editing Logic

#if DEBUG
        var articleEditStart = Stopwatch.StartNew();
#endif

        var editSummary = new StringBuilder();
        editSummary.AppendLine("Automated ship data update.");

        if (mergedData.updatedParameters.Count > 0)
            editSummary.AppendLine("Updated parameters: " + string.Join(", ", mergedData.updatedParameters));

        if (sanitizedData.updatedParameters.Count > 0)
            editSummary.AppendLine("Removed parameters: " + string.Join(", ", sanitizedData.updatedParameters));

        if (!dryRun)
            await bot.EditArticleAsync(ship, newWikitext, editSummary.ToString());

#if DEBUG
        articleEditStart.Stop();
        logger.LogDebug("{Identifier} Edited page in {1}ms", GetShipIdentifier(ship), articleEditStart.ElapsedMilliseconds);
#endif

        #endregion
    }

    /// <summary>
    ///     Used to get a ship identifier for logging purposes. I'm pretty sure this only runs in debug mode, so it should be
    ///     okay to have the overhead from the string formatting.
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

public class ShipAlreadyUpdatedException(string message) : Exception(message);