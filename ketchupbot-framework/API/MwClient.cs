using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using Newtonsoft.Json;

namespace ketchupbot_framework.API;

public class MwClient
{
    private static readonly HttpClient Client = new(new HttpClientHandler
    {
        // Enable cookies for login session
        UseCookies = true,
        CookieContainer = new CookieContainer()
    });

    private readonly string _baseUrl;

    /// <summary>
    /// Client for interacting with the MediaWiki API
    /// </summary>
    /// <param name="username">Username to use for logging in</param>
    /// <param name="password">Password to use for logging in</param>
    /// <param name="baseUrl"></param>
    /// <exception cref="InvalidOperationException"></exception>
    public MwClient(string? username = null, string? password = null, string baseUrl = "https://robloxgalaxy.wiki/api.php")
    {
        _baseUrl = baseUrl;

        Client.DefaultRequestHeaders.Add("User-Agent", $"KetchupBot-Updater/{Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0"}");

        if (username != null && password != null)
            LogIn(username, password).GetAwaiter().GetResult();
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="username"></param>
    /// <param name="password"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    private async Task LogIn(string username, string password)
    {
        using HttpResponseMessage loginTokenRequest = await Client
            .GetAsync($"{_baseUrl}?action=query&format=json&meta=tokens&type=login");

        loginTokenRequest.EnsureSuccessStatusCode();

        string loginTokenJson = await loginTokenRequest.Content.ReadAsStringAsync();
        // Deserialize the JSON response to a dynamic object because I can't be bothered to make a class for it
        dynamic? loginTokenData = JsonConvert.DeserializeObject<dynamic>(loginTokenJson);
        string? loginToken = loginTokenData?.query.tokens.logintoken;

        if (loginToken == null) throw new InvalidOperationException("Failed to fetch login token");

        using HttpResponseMessage loginRequest = await Client.PostAsync(_baseUrl, new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                { "action", "login" },
                { "format", "json" },
                { "lgname", username },
                { "lgpassword", password },
                { "lgtoken", loginToken }
            })
        );

        loginRequest.EnsureSuccessStatusCode();

        string loginJson = await loginRequest.Content.ReadAsStringAsync();
        dynamic? loginData = JsonConvert.DeserializeObject<dynamic>(loginJson);
        if (loginData?.login.result != "Success") throw new InvalidOperationException("Failed to log in to the wiki: " + loginData?.login.reason);
    }

    /// <summary>
    /// Check whether the MwClient is currently logged in or not
    /// </summary>
    /// <returns></returns>
    public async Task<bool> IsLoggedIn()
    {
        using HttpResponseMessage response = await Client.GetAsync($"{_baseUrl}?action=query&format=json&assert=user");

        response.EnsureSuccessStatusCode();

        string jsonResponse = await response.Content.ReadAsStringAsync();

        dynamic? data = JsonConvert.DeserializeObject<dynamic>(jsonResponse);

        return data?.error == null;
    }

    public async Task<string> GetArticle(string title)
    {
        using HttpResponseMessage response = await Client.GetAsync(
            $"{_baseUrl}?action=query&format=json&prop=revisions&titles={HttpUtility.UrlEncode(title)}&rvslots=*&rvprop=content&formatversion=2");

        response.EnsureSuccessStatusCode();

        string jsonResponse = await response.Content.ReadAsStringAsync();

        dynamic? data = JsonConvert.DeserializeObject<dynamic>(jsonResponse);

        return ExtractPageContent(data) ?? throw new InvalidOperationException("Failed to fetch article");
    }

    private static string? ExtractPageContent(dynamic? data)
    {
        if (data == null)
            return null;

        if (data.query?.pages == null || data.query.pages.Count <= 0)
            return null;

        dynamic? page = data.query.pages[0];

        if (page?.revisions == null || page?.revisions.Count <= 0)
            return null;

        dynamic? revision = page?.revisions[0];
        return revision?.slots?.main?.content;
    }

    public async Task EditArticle(string title, string newContent, string summary, bool? dryRun = false)
    {
        // If dry run is enabled, don't actually make the edit. Mock success instead.
        if (dryRun == true) return;

        if (await IsLoggedIn() == false)
            throw new InvalidOperationException("Not logged in");

        // Get MD5 hash of the new content to use for validation
        string newContentHash = BitConverter.ToString(MD5.HashData(Encoding.UTF8.GetBytes(newContent))).Replace("-", "").ToLower();

        using HttpResponseMessage csrfTokenRequest = await Client.GetAsync($"{_baseUrl}?action=query&format=json&meta=tokens");
        csrfTokenRequest.EnsureSuccessStatusCode();
        string csrfTokenJson = await csrfTokenRequest.Content.ReadAsStringAsync();
        dynamic? csrfTokenData = JsonConvert.DeserializeObject<dynamic>(csrfTokenJson);
        string? csrfToken = csrfTokenData?.query.tokens.csrftoken;
        if (csrfToken == null) throw new InvalidOperationException("Failed to fetch CSRF token");

        using HttpResponseMessage editRequest = await Client.PostAsync(_baseUrl, new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                { "action", "edit" },
                { "format", "json" },
                { "title", title },
                { "text", newContent },
                { "bot", "true"},
                { "summary", summary},
                { "md5", newContentHash },
                { "token", csrfToken }
            })
        );

        editRequest.EnsureSuccessStatusCode();

        string editJson = await editRequest.Content.ReadAsStringAsync();
        dynamic? editData = JsonConvert.DeserializeObject<dynamic>(editJson);

        if (editData?.edit?.result != "Success")
            throw new InvalidOperationException("Failed to edit article: " + editData?.edit?.result);
    }
}