using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;

namespace KBot.Framework.API;

public class MediaWikiClient
{
    private readonly string baseUrl;
    private readonly HttpClient httpClient;

    /// <summary>
    ///     Client for interacting with the MediaWiki API
    /// </summary>
    /// <param name="httpClient">
    ///     The <see cref="HttpClient" /> to use when making requests. Ensure that the HttpClient has
    ///     <see cref="HttpClientHandler.UseCookies" /> enabled
    /// </param>
    /// <param name="username">The username to use when logging in</param>
    /// <param name="password">The password to use when logging in</param>
    /// <param name="baseUrl">The url to api.php. Defaults to the Galaxypedia's</param>
    /// <remarks>
    ///     If either username or password are omitted, you will not be logged in. Certain functionality will be inoperable
    ///     until <see cref="LogInAsync" /> is called manually.
    /// </remarks>
    /// <exception cref="InvalidOperationException"></exception>
    public MediaWikiClient(HttpClient httpClient, string? username = null, string? password = null,
        string baseUrl = "https://wiki.galaxy.casa/w/api.php")
    {
        this.baseUrl = baseUrl;
        this.httpClient = httpClient;

        // TODO: This should probably be removed. Callers should be responsible for logging in if they need to
        if (username != null && password != null)
            LogInAsync(username, password).GetAwaiter().GetResult();
    }

    /// <summary>
    /// </summary>
    /// <param name="username"></param>
    /// <param name="password"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task LogInAsync(string username, string password)
    {
        using HttpResponseMessage loginTokenRequest = await httpClient
            .GetAsync($"{baseUrl}?action=query&format=json&meta=tokens&type=login");

        loginTokenRequest.EnsureSuccessStatusCode();

        string loginTokenJson = await loginTokenRequest.Content.ReadAsStringAsync();
        string loginToken = extractLoginToken(loginTokenJson);

        using HttpResponseMessage loginRequest = await httpClient.PostAsync(baseUrl, new FormUrlEncodedContent(
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
        ensureLoginSucceeded(loginJson);
    }

    /// <summary>
    ///     Check whether the MediaWikiClient is currently logged in or not
    /// </summary>
    /// <returns></returns>
    public async Task<bool> IsLoggedInAsync()
    {
        using HttpResponseMessage response =
            await httpClient.GetAsync($"{baseUrl}?action=query&format=json&assert=user");

        response.EnsureSuccessStatusCode();

        string jsonResponse = await response.Content.ReadAsStringAsync();

        using JsonDocument data = JsonDocument.Parse(jsonResponse);

        return data.RootElement.ValueKind == JsonValueKind.Null ||
               !data.RootElement.TryGetProperty("error", out JsonElement error) ||
               error.ValueKind == JsonValueKind.Null;
    }

    /// <summary>
    ///     Get the content of a single article
    /// </summary>
    /// <param name="title"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task<string> GetArticleAsync(string title) =>
        (await GetArticlesAsync([title])).FirstOrDefault().Value ??
        throw new InvalidOperationException("Failed to fetch article " + title);

    /// <summary>
    ///     Get the content of multiple articles
    /// </summary>
    /// <param name="titles"></param>
    /// <returns>An array of page contents. Or an empty array if no pages were found</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task<Dictionary<string, string>> GetArticlesAsync(string[] titles)
    {
        using HttpResponseMessage response = await httpClient.GetAsync(
            $"{baseUrl}?action=query&format=json&prop=revisions&titles={string.Join("|", titles.Select(HttpUtility.UrlEncode))}&rvslots=*&rvprop=content&formatversion=2");

        response.EnsureSuccessStatusCode();

        string jsonResponse = await response.Content.ReadAsStringAsync();

        return parseArticles(jsonResponse);
    }

    /// <summary>
    ///     Edit an article on the wiki with the provided content
    /// </summary>
    /// <param name="title">The title of the page to edit</param>
    /// <param name="newContent">The new content of the page</param>
    /// <param name="summary">The edit summary</param>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task EditArticleAsync(string title, string newContent, string summary)
    {
        if (await IsLoggedInAsync() == false)
            throw new InvalidOperationException("Not logged in");

        #region Hashing

        string newContentHash = BitConverter.ToString(MD5.HashData(Encoding.UTF8.GetBytes(newContent))).Replace("-", "")
            .ToLower();

        using HttpResponseMessage csrfTokenRequest =
            await httpClient.GetAsync($"{baseUrl}?action=query&format=json&meta=tokens");
        csrfTokenRequest.EnsureSuccessStatusCode();
        string csrfTokenJson = await csrfTokenRequest.Content.ReadAsStringAsync();
        string csrfToken = extractCsrfToken(csrfTokenJson);

        #endregion

        using HttpResponseMessage editRequest = await httpClient.PostAsync(baseUrl, new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                { "action", "edit" },
                { "format", "json" },
                { "title", title },
                { "text", newContent },
                { "bot", "true" },
                { "summary", summary },
                { "md5", newContentHash },
                { "token", csrfToken }
            })
        );

        editRequest.EnsureSuccessStatusCode();

        string editJson = await editRequest.Content.ReadAsStringAsync();
        ensureEditSucceeded(editJson);
    }

    private static string extractLoginToken(string json)
    {
        using JsonDocument loginTokenData = JsonDocument.Parse(json);
        string? loginToken = loginTokenData.RootElement.ValueKind == JsonValueKind.Object &&
                             loginTokenData.RootElement.TryGetProperty("query", out JsonElement query) &&
                             query.ValueKind == JsonValueKind.Object &&
                             query.TryGetProperty("tokens", out JsonElement tokens) &&
                             tokens.ValueKind == JsonValueKind.Object &&
                             tokens.TryGetProperty("logintoken", out JsonElement token)
            ? token.GetString()
            : null;

        return loginToken ?? throw new InvalidOperationException("Failed to fetch login token");
    }

    private static void ensureLoginSucceeded(string json)
    {
        using JsonDocument loginData = JsonDocument.Parse(json);
        JsonElement login = default;
        if (loginData.RootElement.ValueKind == JsonValueKind.Object)
            loginData.RootElement.TryGetProperty("login", out login);

        if (login.ValueKind != JsonValueKind.Object ||
            !login.TryGetProperty("result", out JsonElement result) || result.GetString() != "Success")
        {
            string? reason = login.ValueKind == JsonValueKind.Object &&
                             login.TryGetProperty("reason", out JsonElement failureReason)
                ? failureReason.GetString()
                : null;
            throw new InvalidOperationException("Failed to log in to the wiki: " + reason);
        }
    }

    private static Dictionary<string, string> parseArticles(string json)
    {
        using JsonDocument data = JsonDocument.Parse(json);
        if (data.RootElement.ValueKind == JsonValueKind.Null)
            throw new InvalidOperationException("Failed to deserialize response");

        if (!data.RootElement.TryGetProperty("query", out JsonElement query) ||
            query.ValueKind == JsonValueKind.Null ||
            !query.TryGetProperty("pages", out JsonElement pages) || pages.ValueKind == JsonValueKind.Null)
            return [];

        Dictionary<string, string> articles = new();
        foreach (JsonElement page in pages.EnumerateArray())
        {
            string? content = extractPageContent(page);
            if (!string.IsNullOrEmpty(content) && page.TryGetProperty("title", out JsonElement title) &&
                title.ValueKind != JsonValueKind.Null)
                articles.Add(title.ToString(), content);
        }

        return articles;
    }

    private static string? extractPageContent(JsonElement page)
    {
        if (page.ValueKind == JsonValueKind.Null ||
            !page.TryGetProperty("revisions", out JsonElement revisions) ||
            revisions.ValueKind == JsonValueKind.Null || revisions.GetArrayLength() == 0)
            return null;

        JsonElement revision = revisions[0];
        if (revision.ValueKind == JsonValueKind.Null ||
            !revision.TryGetProperty("slots", out JsonElement slots) || slots.ValueKind == JsonValueKind.Null ||
            !slots.TryGetProperty("main", out JsonElement main) || main.ValueKind == JsonValueKind.Null ||
            !main.TryGetProperty("content", out JsonElement content))
            return null;

        return content.GetString();
    }

    private static string extractCsrfToken(string json)
    {
        using JsonDocument data = JsonDocument.Parse(json);
        if (data.RootElement.ValueKind == JsonValueKind.Object &&
            data.RootElement.TryGetProperty("query", out JsonElement query) && query.ValueKind == JsonValueKind.Object &&
            query.TryGetProperty("tokens", out JsonElement tokens) && tokens.ValueKind == JsonValueKind.Object &&
            tokens.TryGetProperty("csrftoken", out JsonElement token) && token.GetString() is { } csrfToken)
            return csrfToken;

        throw new InvalidOperationException("Failed to fetch CSRF token");
    }

    private static void ensureEditSucceeded(string json)
    {
        using JsonDocument data = JsonDocument.Parse(json);
        if (data.RootElement.ValueKind == JsonValueKind.Null)
            throw new InvalidOperationException("Failed to deserialize edit response");

        if (!data.RootElement.TryGetProperty("edit", out JsonElement edit) || edit.ValueKind != JsonValueKind.Object ||
            !edit.TryGetProperty("result", out JsonElement result) || result.GetString() != "Success")
            throw new InvalidOperationException("Failed to edit article: " + data.RootElement);
    }
}
