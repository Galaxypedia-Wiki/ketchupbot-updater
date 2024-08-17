using Serilog;

namespace ketchupbot_framework.API;

public static class HealthChecks
{
    private static readonly HttpClient Client = new();

    public static async Task Ping(string url)
    {
        try
        {
            HttpResponseMessage response = await Client.GetAsync(url);
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException e)
        {
            Log.Error(e, "Failed to ping {Url}", url);
        }
    }
}