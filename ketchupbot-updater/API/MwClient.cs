using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using Newtonsoft.Json;

namespace ketchupbot_updater.API;

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
    public MwClient(string username, string password, string baseUrl = "https://robloxgalaxy.wiki/api.php")
    {
        _baseUrl = baseUrl;

        Client.DefaultRequestHeaders.Add("User-Agent", "KetchupBot-Updater/1.0");

        using HttpResponseMessage loginTokenRequest = Client
            .GetAsync($"{baseUrl}?action=query&format=json&meta=tokens&type=login").GetAwaiter().GetResult();

        loginTokenRequest.EnsureSuccessStatusCode();

        string loginTokenJson = loginTokenRequest.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        // Deserialize the JSON response to a dynamic object because I can't be bothered to make a class for it
        dynamic? loginTokenData = JsonConvert.DeserializeObject<dynamic>(loginTokenJson);
        string? loginToken = loginTokenData?.query.tokens.logintoken;

        if (loginToken == null) throw new InvalidOperationException("Failed to fetch login token");

        using HttpResponseMessage loginRequest = Client.PostAsync(baseUrl, new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                { "action", "login" },
                { "format", "json" },
                { "lgname", username },
                { "lgpassword", password },
                { "lgtoken", loginToken }
            })
        ).GetAwaiter().GetResult();

        loginRequest.EnsureSuccessStatusCode();

        string loginJson = loginRequest.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        dynamic? loginData = JsonConvert.DeserializeObject<dynamic>(loginJson);
        if (loginData?.login.result != "Success") throw new InvalidOperationException("Failed to log in to the wiki: " + loginData?.login.reason);
    }

    public async Task<string> GetArticle(string title)
    {

        using HttpResponseMessage response = await Client.GetAsync($"{_baseUrl}?action=query&format=json&prop=revisions&titles={HttpUtility.UrlEncode(title)}&rvslots=*&rvprop=content&formatversion=2");

        response.EnsureSuccessStatusCode();

        string jsonResponse = await response.Content.ReadAsStringAsync();

        dynamic? data = JsonConvert.DeserializeObject<dynamic>(jsonResponse);

        // TODO: Fix the code duplication here

        string? pageContent = null;

        if (data?.query?.pages == null || data?.query.pages.Count <= 0)
            return pageContent ?? throw new InvalidOperationException("Failed to fetch article");

        dynamic? page = data?.query.pages[0];

        if (page?.revisions == null || page?.revisions.Count <= 0)
            return pageContent ?? throw new InvalidOperationException("Failed to fetch article");

        dynamic? revision = page?.revisions[0];

        if (revision?.slots?.main != null) pageContent = revision.slots.main.content;

        return pageContent ?? throw new InvalidOperationException("Failed to fetch article");
    }

    public async Task<bool> EditArticle(string title, string newContent, string summary)
    {
        // If dry run is enabled, don't actually make the edit. Mock success instead.
        if (Program.DryRun)
            return true;

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
        return editData?.edit?.result == "Success";
    }
}