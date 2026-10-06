using System.Net;
using System.Text.Json;
using KBot.Framework.API;

namespace KBot.Tests.API;

public class MediaWikiClientTests
{
    [Theory]
    [InlineData("{}", true)]
    [InlineData("""{"batchcomplete":""}""", true)]
    [InlineData("""{"error":{"code":"assertuserfailed"}}""", false)]
    [InlineData("""{"error":null}""", true)]
    [InlineData("null", true)]
    public async Task IsLoggedIn_ChecksErrorProperty(string json, bool expected)
    {
        using var client = new HttpClient(new ResponseHandler(json));
        var wiki = new MediaWikiClient(client, baseUrl: "https://example.invalid/api.php");

        Assert.Equal(expected, await wiki.IsLoggedInAsync());
    }

    [Fact]
    public async Task IsLoggedIn_InvalidJsonThrows()
    {
        using var client = new HttpClient(new ResponseHandler("invalid"));
        var wiki = new MediaWikiClient(client, baseUrl: "https://example.invalid/api.php");

        await Assert.ThrowsAnyAsync<JsonException>(() => wiki.IsLoggedInAsync());
    }

    [Fact]
    public async Task IsLoggedIn_HttpFailureThrows()
    {
        using var client = new HttpClient(new ResponseHandler("{}", HttpStatusCode.ServiceUnavailable));
        var wiki = new MediaWikiClient(client, baseUrl: "https://example.invalid/api.php");

        await Assert.ThrowsAsync<HttpRequestException>(() => wiki.IsLoggedInAsync());
    }

    private sealed class ResponseHandler(string json, HttpStatusCode status = HttpStatusCode.OK) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.Equal("?action=query&format=json&assert=user", request.RequestUri!.Query);
            return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(json) });
        }
    }
}
