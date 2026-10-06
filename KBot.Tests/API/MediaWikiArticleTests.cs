using System.Net;
using KBot.Framework.API;

namespace KBot.Tests.API;

public class MediaWikiArticleTests
{
    [Fact]
    public async Task GetArticles_ReadsContentAndSkipsMissingPages()
    {
        using var client = new HttpClient(new ResponseHandler("""
            {"query":{"pages":[
              {"title":"A","revisions":[{"slots":{"main":{"content":"Text A"}}}]},
              {"title":"B","missing":true},
              {"title":"C","revisions":[]},
              {"title":"D","revisions":[{"slots":{"main":{"content":""}}}]},
              {"revisions":[{"slots":{"main":{"content":"No title"}}}]}
            ]}}
            """));
        var wiki = new MediaWikiClient(client, baseUrl: "https://example.invalid/api.php");
        Dictionary<string, string> articles = await wiki.GetArticlesAsync(["A", "B"]);
        Assert.Single(articles);
        Assert.Equal("Text A", articles["A"]);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("""{"query":null}""")]
    [InlineData("""{"query":{"pages":[]}}""")]
    [InlineData("""{"query":{"pages":[null,{"title":"A","revisions":[null]}]}}""")]
    [InlineData("""{"query":{"pages":[{"title":"A","revisions":[{"slots":{"main":{}}}]}]}}""")]
    public async Task GetArticles_NoContentReturnsEmpty(string json)
    {
        using var client = new HttpClient(new ResponseHandler(json));
        Assert.Empty(await new MediaWikiClient(client, baseUrl: "https://example.invalid/api.php").GetArticlesAsync(["A"]));
    }

    [Fact]
    public async Task GetArticles_NullResponseThrows()
    {
        using var client = new HttpClient(new ResponseHandler("null"));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new MediaWikiClient(client, baseUrl: "https://example.invalid/api.php").GetArticlesAsync(["A"]));
    }

    private sealed class ResponseHandler(string json) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Contains("prop=revisions", request.RequestUri!.Query);
            Assert.Contains("formatversion=2", request.RequestUri.Query);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) });
        }
    }
}
