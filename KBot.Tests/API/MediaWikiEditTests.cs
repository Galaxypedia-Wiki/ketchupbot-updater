using System.Net;
using KBot.Framework.API;

namespace KBot.Tests.API;

public class MediaWikiEditTests
{
    [Fact]
    public async Task EditArticle_PostsContentTokenAndHash()
    {
        using var handler = new EditHandler("{}", """{"query":{"tokens":{"csrftoken":"test-token"}}}""",
            """{"edit":{"result":"Success"}}""");
        using var client = new HttpClient(handler);
        await new MediaWikiClient(client, baseUrl: "https://example.invalid/api.php").EditArticleAsync("A", "hello", "test");
        Assert.Equal(3, handler.Requests);
    }

    [Theory]
    [InlineData("""{"error":{"code":"assertuserfailed"}}""", "{}", "{}", "Not logged in", 1)]
    [InlineData("{}", "{}", "{}", "Failed to fetch CSRF token", 2)]
    [InlineData("{}", """{"query":{"tokens":{"csrftoken":null}}}""", "{}", "Failed to fetch CSRF token", 2)]
    [InlineData("{}", """{"query":{"tokens":{"csrftoken":"test-token"}}}""", "null", "Failed to deserialize edit response", 3)]
    [InlineData("{}", """{"query":{"tokens":{"csrftoken":"test-token"}}}""", """{"error":{"code":"badtoken"}}""", "Failed to edit article:", 3)]
    public async Task EditArticle_FailureStopsAtExpectedStage(string login, string token, string edit, string message, int requests)
    {
        using var handler = new EditHandler(login, token, edit);
        using var client = new HttpClient(handler);
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new MediaWikiClient(client, baseUrl: "https://example.invalid/api.php").EditArticleAsync("A", "hello", "test"));
        Assert.StartsWith(message, exception.Message);
        Assert.Equal(requests, handler.Requests);
    }

    private sealed class EditHandler(string login, string token, string edit) : HttpMessageHandler
    {
        public int Requests { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests++;
            if (Requests == 3)
            {
                Assert.Equal(HttpMethod.Post, request.Method);
                string form = await request.Content!.ReadAsStringAsync(cancellationToken);
                Assert.Contains("title=A", form);
                Assert.Contains("text=hello", form);
                Assert.Contains("summary=test", form);
                Assert.Contains("token=test-token", form);
                Assert.Contains("md5=5d41402abc4b2a76b9719d911017c592", form);
            }
            else
            {
                Assert.Equal(HttpMethod.Get, request.Method);
                Assert.Equal(Requests == 1 ? "?action=query&format=json&assert=user" : "?action=query&format=json&meta=tokens", request.RequestUri!.Query);
            }
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(Requests switch { 1 => login, 2 => token, _ => edit })
            };
        }
    }
}
