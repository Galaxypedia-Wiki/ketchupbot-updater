using System.Web;
using ketchupbot_updater.Types;
using Newtonsoft.Json;

namespace ketchupbot_updater.API;

public class ApiManager(string galaxyInfoApi, string galaxyInfoApiToken)
{
    /// <summary>
    /// Cached ship data from the last successful run.
    /// TODO: This probably isn't enough. We should probably add persistence to this.
    /// </summary>
    private string? _cachedShipData;

    private static readonly HttpClient HttpClient = new();

    /// <summary>
    /// Get ship data from the Galaxy Info API
    /// </summary>
    /// <param name="returnCachedOnError">Whether to return an internally cached version (from the last successful run, if any), if the request fails. If false, or if there is no cached version available, an <see cref="HttpRequestException"/> will be raised on error.</param>
    /// <returns>A list of ships from the Galaxy Info API with their respective data attributes</returns>
    public async Task<Dictionary<string, ShipData>?> GetShipsData(bool returnCachedOnError = true)
    {
        using HttpResponseMessage response = await HttpClient.GetAsync(
            $"{galaxyInfoApi.Trim()}/api/v2/galaxypedia?token={HttpUtility.UrlEncode(galaxyInfoApiToken.Trim())}");

        switch (response.IsSuccessStatusCode)
        {
            case false when returnCachedOnError && _cachedShipData != null:
                // Status code is not successful, return cached data option is enabled, and there is cached data available
                return JsonConvert.DeserializeObject<Dictionary<string, ShipData>>(_cachedShipData);
            case false:
                // Status code is not successful, and either the return cached data option is disabled or there is no cached data available
                throw new HttpRequestException($"Failed to fetch ship data from the Galaxy Info API: {response.ReasonPhrase}");
            default:
            {
                // Status code is successful, update the cached data
                string jsonResponse = await response.Content.ReadAsStringAsync();

                _cachedShipData = jsonResponse;

                return JsonConvert.DeserializeObject<Dictionary<string, ShipData>>(jsonResponse);
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