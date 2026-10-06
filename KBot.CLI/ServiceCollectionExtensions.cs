using System.Net;
using KBot.Framework;
using KBot.Framework.API;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using Serilog;

namespace KBot.CLI;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUpdaterServices(this IServiceCollection services, bool dryRun, bool turrets, bool verbose = false)
    {
        services.AddSerilog();
        services.AddMemoryCache();
        services.AddSingleton<UpdaterRunner>();

        #region HttpClient

        string userAgent =
            $"KetchupBot-Updater/{ApplicationVersion.Value}";

        services.AddHttpClient<ApiManager>(client =>
            {
                client.DefaultRequestHeaders.Add("User-Agent", userAgent);
            })
            // Galaxy Info's public API tends to go down rather frequently, and for hours at a time, so we go the
            // route of retrying forever within a certain time frame. So a timeout of 5 minutes should be enough to
            // consider the request as still achievable. Anything longer than that is a lost cause.
            .AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromMinutes(5)))
            .AddPolicyHandler(HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryForeverAsync(retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)) +
                    TimeSpan.FromMilliseconds(new Random().Next(0, 100))));

        services.AddHttpClient<MediaWikiClient>(client =>
        {
            client.DefaultRequestHeaders.Add("User-Agent", userAgent);
        }).ConfigurePrimaryHttpMessageHandler(() =>
            new HttpClientHandler
            {
                UseCookies = true,
                // We create a new CookieContainer for each client to avoid cookie conflicts (i.e. two clients on two different accounts)
                CookieContainer = new CookieContainer()
            }).AddPolicyHandler(HttpPolicyExtensions.HandleTransientHttpError()
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));

        #endregion

        services.AddSingleton<MediaWikiClient>(provider =>
        {
            provider.GetRequiredService<IConfiguration>();

            return new MediaWikiClient(
                provider.GetRequiredService<IHttpClientFactory>().CreateClient("MediaWikiClient"),
                provider.GetRequiredService<IConfiguration>()["MWUSERNAME"] ??
                throw new InvalidOperationException("MWUSERNAME not set"),
                provider.GetRequiredService<IConfiguration>()["MWPASSWORD"] ??
                throw new InvalidOperationException("MWPASSWORD not set"));
        });

        services.AddSingleton<ApiManager>(provider =>
        {
            provider.GetRequiredService<IConfiguration>();
            return new ApiManager(
                provider.GetRequiredService<IConfiguration>()["GIAPI_URL"] ??
                throw new InvalidOperationException("GIAPI_URL not set"),
                provider.GetRequiredService<IHttpClientFactory>().CreateClient("ApiManager"),
                provider.GetRequiredService<IMemoryCache>()
            );
        });

        services.AddSingleton<ShipUpdater>(provider => new ShipUpdater(
            provider.GetRequiredService<MediaWikiClient>(),
            provider.GetRequiredService<ApiManager>(),
            provider.GetRequiredService<ILogger<ShipUpdater>>(),
            dryRun, verbose));

        if (turrets)
            services.AddSingleton<TurretUpdater>(provider => new TurretUpdater(
                provider.GetRequiredService<MediaWikiClient>(),
                provider.GetRequiredService<ApiManager>()));

        return services;
    }
}
