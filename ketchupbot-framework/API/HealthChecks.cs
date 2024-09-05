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

    public static async Task BroadcastInProgress(string url)
    {
        try
        {
            HttpResponseMessage response = await Client.GetAsync(url + "?status=in_progress");
            response.EnsureSuccessStatusCode();
            Log.Information("Broadcasted in progress to {Url}", url);;
        }
        catch (HttpRequestException e)
        {
            Log.Error(e, "Failed to broadcast in progress to {Url}", url);
        }
    }

    public static async Task BroadcastComplete(string url)
    {
        try
        {
            HttpResponseMessage response = await Client.GetAsync(url + "?status=ok");
            response.EnsureSuccessStatusCode();
            Log.Information("Broadcasted complete to {Url}", url);
        }
        catch (HttpRequestException e)
        {
            Log.Error(e, "Failed to broadcast completed to {Url}", url);
        }
    }

    public static async Task BroadcastFailure(string url)
    {
        try
        {
            HttpResponseMessage response = await Client.GetAsync(url + "?status=error");
            response.EnsureSuccessStatusCode();
            Log.Information("Broadcasted failure to {Url}", url);
        }
        catch (HttpRequestException e)
        {
            Log.Error(e, "Failed to broadcast failed to {Url}", url);
        }
    }
}