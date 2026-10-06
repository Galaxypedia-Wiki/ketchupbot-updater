using System.Net;
using System.Text.Json;
using KBot.Framework.API;
using KBot.Framework.Types;

namespace KBot.Tests.API;

public class TurretDataTests
{
    [Theory]
    [InlineData("Beam", TurretTypeEnum.Beam)]
    [InlineData("PDL", TurretTypeEnum.Pdl)]
    [InlineData("Cannon", TurretTypeEnum.Cannon)]
    public async Task GetTurretData_ReadsUpstreamShape(string type, TurretTypeEnum expected)
    {
        string json = $$$$"""
            {"serializedShips":{},"serializedTurrets":{"UCan":{"DPS":112.9150390625,"Reload":1.96608,"Range":5223.02133,"Damage":28.8,"BeamSize":3.5,"BaseAccuracy":0.0181,"AccuracyIndex":0.05197,"RampingStrength":0.1,"SpeedDenominator":2952.45,"Cost":{"3":394},"NumBarrels":6,"TurretType":"{{{{type}}}}"}}}
            """;
        using var client = new HttpClient(new ResponseHandler(json));
        var manager = new ApiManager("https://example.invalid", client, null);

        TurretData turret = (await manager.GetTurretData())!["UCan"];

        Assert.Equal(112.9150390625, turret.Dps);
        Assert.Equal(1.96608, turret.Reload);
        Assert.Equal(5223.02133, turret.Range);
        Assert.Equal(28.8, turret.Damage);
        Assert.Equal(3.5, turret.BeamSize);
        Assert.Equal(0.0181, turret.BaseAccuracy);
        Assert.Equal(0.05197, turret.AccuracyIndex);
        Assert.Equal(0.1, turret.RampingStrength);
        Assert.Equal(2952.45, turret.SpeedDenominator);
        Assert.Equal(394, turret.Cost[3]);
        Assert.Equal(6, turret.NumBarrels);
        Assert.Equal(expected, turret.TurretType);
    }

    [Fact]
    public async Task GetTurretData_NullPayloadReturnsNull()
    {
        using var client = new HttpClient(new ResponseHandler("""{"serializedTurrets":null}"""));
        Assert.Null(await new ApiManager("https://example.invalid", client, null).GetTurretData());
    }

    [Fact]
    public async Task GetTurretData_MissingPayloadFailsClearly()
    {
        using var client = new HttpClient(new ResponseHandler("{}"));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new ApiManager("https://example.invalid", client, null).GetTurretData());
    }

    [Theory]
    [InlineData("invalid json")]
    [InlineData("""{"serializedTurrets":{"T":{"TurretType":"Unknown"}}}""")]
    public async Task GetTurretData_InvalidPayloadFails(string json)
    {
        using var client = new HttpClient(new ResponseHandler(json));
        await Assert.ThrowsAnyAsync<JsonException>(() =>
            new ApiManager("https://example.invalid", client, null).GetTurretData());
    }

    private sealed class ResponseHandler(string json) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.Equal("/api/v2/ships-turrets/raw", request.RequestUri!.AbsolutePath);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) });
        }
    }
}
