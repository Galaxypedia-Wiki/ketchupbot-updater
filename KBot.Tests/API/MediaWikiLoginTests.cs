using System.Net;
using KBot.Framework.API;

namespace KBot.Tests.API;

public class MediaWikiLoginTests
{
    [Fact]
    public async Task LogIn_UsesTokenAndCredentials()
    {
        using var handler = new LoginHandler("""{"query":{"tokens":{"logintoken":"test-token"}}}""",
            """{"login":{"result":"Success"}}""");
        using var client = new HttpClient(handler);
        await new MediaWikiClient(client, baseUrl: "https://example.invalid/api.php").LogInAsync("test-user", "test-password");
        Assert.Equal(2, handler.Requests);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("null")]
    [InlineData("""{"query":{"tokens":{"logintoken":null}}}""")]
    public async Task LogIn_MissingTokenStopsBeforePosting(string response)
    {
        using var handler = new LoginHandler(response, "{}");
        using var client = new HttpClient(handler);
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new MediaWikiClient(client, baseUrl: "https://example.invalid/api.php").LogInAsync("test-user", "test-password"));
        Assert.Equal("Failed to fetch login token", exception.Message);
        Assert.Equal(1, handler.Requests);
    }

    [Theory]
    [InlineData("""{"login":{"result":"Failed","reason":"Wrong password"}}""", "Wrong password")]
    [InlineData("{}", "")]
    [InlineData("null", "")]
    public async Task LogIn_FailedResultReportsReason(string response, string reason)
    {
        using var handler = new LoginHandler("""{"query":{"tokens":{"logintoken":"test-token"}}}""", response);
        using var client = new HttpClient(handler);
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new MediaWikiClient(client, baseUrl: "https://example.invalid/api.php").LogInAsync("test-user", "test-password"));
        Assert.Equal("Failed to log in to the wiki: " + reason, exception.Message);
    }

    private sealed class LoginHandler(string tokenResponse, string loginResponse) : HttpMessageHandler
    {
        public int Requests { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests++;
            if (Requests == 1)
            {
                Assert.Equal(HttpMethod.Get, request.Method);
                Assert.Equal("?action=query&format=json&meta=tokens&type=login", request.RequestUri!.Query);
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(tokenResponse) };
            }

            Assert.Equal(HttpMethod.Post, request.Method);
            string form = await request.Content!.ReadAsStringAsync(cancellationToken);
            Assert.Contains("lgname=test-user", form);
            Assert.Contains("lgpassword=test-password", form);
            Assert.Contains("lgtoken=test-token", form);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(loginResponse) };
        }
    }
}
