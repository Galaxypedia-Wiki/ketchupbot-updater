using System.Net;
using KBot.Framework.API;
using Microsoft.Extensions.Caching.Memory;

namespace KBot.Tests.API;

public class ApiManagerTests
{
    [Fact]
    public async Task GetShipsData_PreservesScalarValuesAndKeys()
    {
        using var client = new HttpClient(new ResponseHandler("""
            {"Ship":{"name":"Example","mass":42,"enabled":true,"missing":null,"empty":""}}
            """));
        var manager = new ApiManager("https://example.invalid", client, null);

        Dictionary<string, string> ship = (await manager.GetShipsData())["Ship"];

        Assert.Equal("Example", ship["name"]);
        Assert.Equal("42", ship["mass"]);
        Assert.Equal("true", ship["enabled"]);
        Assert.Null(ship["missing"]);
        Assert.Equal("", ship["empty"]);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("invalid json")]
    public async Task GetShipsData_InvalidResponseFails(string json)
    {
        using var client = new HttpClient(new ResponseHandler(json));
        var manager = new ApiManager("https://example.invalid", client, null);

        await Assert.ThrowsAsync<InvalidOperationException>(() => manager.GetShipsData());
    }

    [Fact]
    public async Task GetShipsData_FailedResponseReturnsCachedData()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var expected = new Dictionary<string, Dictionary<string, string>> { ["Ship"] = new() };
        cache.Set("ShipData", expected);
        using var client = new HttpClient(new ResponseHandler("", HttpStatusCode.ServiceUnavailable));
        var manager = new ApiManager("https://example.invalid", client, cache);

        Assert.Same(expected, await manager.GetShipsData());
    }

    private sealed class ResponseHandler(string json, HttpStatusCode status = HttpStatusCode.OK) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.Equal("/api/v2/galaxypedia", request.RequestUri!.AbsolutePath);
            return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(json) });
        }
    }
}
