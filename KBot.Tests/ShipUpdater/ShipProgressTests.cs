using System.Net;
using KBot.Framework;
using KBot.Framework.API;
using Microsoft.Extensions.Logging;

namespace KBot.Tests.ShipUpdater;

public class ShipProgressTests
{
    [Theory]
    [InlineData(false, 0)]
    [InlineData(true, 2)]
    public async Task MissingShipsCountAsFailuresAndOnlyLogWhenVerbose(bool verbose, int errors)
    {
        using var http = new HttpClient(new FakeHandler());
        var logger = new CaptureLogger();
        var updater = new Framework.ShipUpdater(new MediaWikiClient(http),
            new ApiManager("https://example.invalid", http, null), logger, true, verbose);
        var reports = new List<ShipUpdateProgress>();
        await updater.MassUpdateShips(["Missing A", "Missing B"], new(), progress: reports.Add);
        Assert.Equal(2, reports[^1].Failed);
        Assert.Equal(2, reports[^1].Completed);
        Assert.Equal(0, reports[^1].Updated);
        Assert.Equal(errors, logger.Errors);
        Assert.Equal(new[] { 0, 1, 2 }, reports.Select(report => report.Completed));
    }

    private sealed class FakeHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            { Content = new StringContent(request.RequestUri!.Query.Length > 0 ? "{\"query\":{\"pages\":[]}}" : "{}") });
    }

    private sealed class CaptureLogger : ILogger<Framework.ShipUpdater>
    {
        public int Errors;
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel level) => true;
        public void Log<TState>(LogLevel level, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (level == LogLevel.Error) Interlocked.Increment(ref Errors);
        }
    }
}
