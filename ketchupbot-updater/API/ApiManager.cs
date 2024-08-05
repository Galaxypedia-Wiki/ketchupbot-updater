using System.Reflection;
using ketchupbot_updater.Types;
using Newtonsoft.Json;

namespace ketchupbot_updater.API;

/// <summary>
/// API Manager for interacting with the Galaxy Info API. Responsible for fetching, processing, and returning properly formatted data from the Galaxy Info API.
/// </summary>
/// <param name="galaxyInfoApi"></param>
public class ApiManager(string galaxyInfoApi)
{
    /// <summary>
    /// Cached ship data from the last successful run.
    /// TODO: This probably isn't enough. We should probably add persistence to this. Might also be wise to have GetShipsData always return the cached value, and only update the cached value once per hour.
    /// </summary>
    private Dictionary<string, Dictionary<string, string>>? _cachedShipData;

    private static readonly HttpClient HttpClient = new();

    static ApiManager()
    {
        HttpClient.DefaultRequestHeaders.Add("User-Agent", $"KetchupBot-Updater/{Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0"}");
    }

    /// <summary>
    /// Get ship data from the Galaxy Info API
    /// </summary>
    /// <param name="returnCachedOnError">Whether to return an internally cached version (from the last successful run, if any), if the request fails. If false, or if there is no cached version available, an <see cref="HttpRequestException"/> will be raised on error.</param>
    /// <returns>A list of ships from the Galaxy Info API with their respective data attributes</returns>
    public async Task<Dictionary<string, Dictionary<string, string>>?> GetShipsData(bool returnCachedOnError = true)
    {
        using HttpResponseMessage response = await HttpClient.GetAsync($"{galaxyInfoApi.Trim()}/api/v2/galaxypedia");

        switch (response.IsSuccessStatusCode)
        {
            case false when returnCachedOnError && _cachedShipData != null:
                // Status code is not successful, return cached data option is enabled, and there is cached data available
                return _cachedShipData;
            case false:
                // Status code is not successful, and either the return cached data option is disabled or there is no cached data available
                throw new HttpRequestException(
                    $"Failed to fetch ship data from the Galaxy Info API: {response.ReasonPhrase}");
            default:
            {
                // Status code is successful, update the cached data
                string jsonResponse = await response.Content.ReadAsStringAsync();

                // Deserialize the response into a Dictionary<string, Dictionary<string, string>> because the json is formatted as:
                // {
                //   "shiptitle":
                //   {
                //     "attribute": "value",
                //   }
                // }
                var deserializedResponse = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(jsonResponse);

                _cachedShipData = deserializedResponse;

                return deserializedResponse;
            }
        }
    }

    public async Task<Dictionary<string, TurretData>?> GetTurretData()
    {
        HttpResponseMessage response = await HttpClient.GetAsync($"{galaxyInfoApi.Trim()}/api/v2/ships-turret/raw");

        response.EnsureSuccessStatusCode();

        string stringResponse = await response.Content.ReadAsStringAsync();

        Console.WriteLine(stringResponse);

        dynamic? jsonResponse = JsonConvert.DeserializeObject<dynamic>(stringResponse);

        return JsonConvert.DeserializeObject<Dictionary<string, TurretData>>(jsonResponse?.serializedTurrets);
    }
}