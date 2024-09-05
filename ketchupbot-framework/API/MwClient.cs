using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ketchupbot_framework.API;

public class MwClient
{
    protected static readonly HttpClient Client = new(new HttpClientHandler
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
    public MwClient(string? username = null, string? password = null, string baseUrl = "https://galaxypedia.org/api.php")
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

    /// <summary>
    /// Get the content of a single article
    /// </summary>
    /// <param name="title"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task<string> GetArticle(string title)
    {
        return (await GetArticles([title])).FirstOrDefault().Value ??
               throw new InvalidOperationException("Failed to fetch article");
    }

    /// <summary>
    /// Get the content of multiple articles
    /// </summary>
    /// <param name="titles"></param>
    /// <returns>An array of page contents. Or an empty array if no pages were found</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task<Dictionary<string, string>> GetArticles(string[] titles)
    {
        using HttpResponseMessage response = await Client.GetAsync(
            $"{_baseUrl}?action=query&format=json&prop=revisions&titles={string.Join("|", titles.Select(HttpUtility.UrlEncode))}&rvslots=*&rvprop=content&formatversion=2");

        response.EnsureSuccessStatusCode();

        string jsonResponse = await response.Content.ReadAsStringAsync();

        dynamic data = JsonConvert.DeserializeObject<dynamic>(jsonResponse) ?? throw new InvalidOperationException("Failed to deserialize response");

        JArray? pages = data.query?.pages;

        if (pages == null || pages.Count == 0)
            return [];

        Dictionary<string, string> articles = new();

        foreach (dynamic page in pages)
        {
            string? content = ExtractPageContent(page);
            if (!string.IsNullOrEmpty(content) && page.title != null)
                articles.Add(page.title.ToString(), content);
        }

        return articles;
    }

    private static string? ExtractPageContent(dynamic page)
    {
        if (page == null)
            return null;

        if (page.revisions == null || page.revisions.Count <= 0)
            return null;

        dynamic? revision = page.revisions[0];
        return revision?.slots?.main?.content;
    }

    /// <summary>
    /// Edit an article on the wiki with the provided content
    /// </summary>
    /// <param name="title">The title of the page to edit</param>
    /// <param name="newContent">The new content of the page</param>
    /// <param name="summary">The edit summary</param>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task EditArticle(string title, string newContent, string summary)
    {
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