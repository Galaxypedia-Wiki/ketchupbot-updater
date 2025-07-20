using System.CommandLine;
using System.Net;
using System.Reflection;
using ketchupbot_framework;
using ketchupbot_framework.API;
using ketchupbot_updater.Jobs;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Polly;
using Polly.Extensions.Http;
using Quartz;
using Quartz.Impl;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

namespace ketchupbot_updater;

/// <summary>
///     Entrypoint, configuration, and initialization for the updater component
/// </summary>
public class Program
{
    private static bool DryRun { get; set; }

    private static async Task<int> Main(string[] args)
    {
        #region Options

        var shipsOption = new Option<string[]>(
            ["--ships", "-s"],
            () => ["all"],
            """List of ships to update. "all" to update all ships, "none" to not update ships. Must be "all" in order to use the scheduler"""
        )
        {
            AllowMultipleArgumentsPerToken = true
        };

        var turretsOption = new Option<bool>(
            ["-t", "--turrets"],
            "Update turrets?"
        );

        var shipScheduleOption = new Option<string>(
            ["-ss", "--ship-schedule"],
            "Pass a schedule (in cron format specialized for quartz) to enable the ship scheduler. Use https://www.freeformatter.com/cron-expression-generator-quartz.html to generate a cron expression. Can pass Will ignore --ships option. This takes precedence over the environment variable"
        );

        var turretScheduleOption = new Option<string>(
            ["-ts", "--turret-schedule"],
            "Pass to enable the turret scheduler. Will ignore the --turrets option. This takes precedence over the environment variable"
        );

        var dryRunOption = new Option<bool>(
            "--dry-run",
            "Pass to enable dry run. Only works in production due to some stupid technical foresight. This takes precedence over the environment variable"
        );

        var threadCountOption = new Option<int>(
            "--threads",
            () => 0,
            "Number of threads to use when updating ships. Set to 1 for single threaded execution, 0 for automatic (let the the .NET runtime manage the thread count dynamically).");

        var secretsDirectoryOption = new Option<string>(
            "--secrets-directory",
            () => AppContext.BaseDirectory,
            "Directory where the appsettings.json file is located. Defaults to the directory holding the executable."
        );

        var verboseOption = new Option<bool>(
            ["-v", "--verbose"],
            "Enable verbose logging"
        );

        #endregion

        var rootCommand = new RootCommand("KetchupBot Updater Component")
        {
            shipsOption,
            turretsOption,
            shipScheduleOption,
            turretScheduleOption,
            dryRunOption,
            threadCountOption,
            secretsDirectoryOption,
            verboseOption
        };

        var levelSwitch = new LoggingLevelSwitch();

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.ControlledBy(levelSwitch)
#if !DEBUG
            .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
#endif
            .WriteTo.Console(theme: SystemConsoleTheme.Grayscale)
            .CreateLogger();

        rootCommand.SetHandler(async handler =>
        {
            await using (Stream? stream = Assembly.GetExecutingAssembly()
                             .GetManifestResourceStream("ketchupbot_updater.Assets.banner.txt"))
            {
                if (stream == null)
                    throw new InvalidOperationException("Failed to load banner");

                using (var reader = new StreamReader(stream))
                {
                    Console.WriteLine(await reader.ReadToEndAsync());
                }
            }

            Console.WriteLine(
                $"\nketchupbot-updater | v{Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Development"} | {DateTime.Now}\n");

            HostApplicationBuilder applicationBuilder = Host.CreateApplicationBuilder(args);
            applicationBuilder.Services.AddQuartz();
            applicationBuilder.Services.AddQuartzHostedService();
            applicationBuilder.Services.AddSerilog();
            applicationBuilder.Services.AddMemoryCache();
            applicationBuilder.Configuration
                .AddUserSecrets<Program>()
                .AddEnvironmentVariables()
                .AddJsonFile("appsettings.json",
                    true,
                    true);

            #region Configuration

#if DEBUG
            Log.Information("Running in development mode");
            levelSwitch.MinimumLevel = LogEventLevel.Debug;
            DryRun = true;
#else
            DryRun = handler.ParseResult.GetValueForOption(dryRunOption);
            if (DryRun) Log.Information("Running in dry run mode");
#endif

            if (handler.ParseResult.GetValueForOption(verboseOption))
            {
                levelSwitch.MinimumLevel = LogEventLevel.Verbose;
                Log.Information("Enabled verbose logging");
            }

            #endregion

            #region Dependencies

            #region HttpClient

            string userAgent =
                $"KetchupBot-Updater/{Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0"}";

            applicationBuilder.Services.AddHttpClient<ApiManager>(client =>
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

            applicationBuilder.Services.AddHttpClient<MediaWikiClient>(client =>
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

            applicationBuilder.Services.AddSingleton<MediaWikiClient>(provider =>
            {
                provider.GetRequiredService<IConfiguration>();

                return new MediaWikiClient(
                    provider.GetRequiredService<IHttpClientFactory>().CreateClient("MediaWikiClient"),
                    provider.GetRequiredService<IConfiguration>()["MWUSERNAME"] ??
                    throw new InvalidOperationException("MWUSERNAME not set"),
                    provider.GetRequiredService<IConfiguration>()["MWPASSWORD"] ??
                    throw new InvalidOperationException("MWPASSWORD not set"));
            });

            applicationBuilder.Services.AddSingleton<ApiManager>(provider =>
            {
                provider.GetRequiredService<IConfiguration>();
                return new ApiManager(
                    provider.GetRequiredService<IConfiguration>()["GIAPI_URL"] ??
                    throw new InvalidOperationException("GIAPI_URL not set"),
                    provider.GetRequiredService<IHttpClientFactory>().CreateClient("ApiManager"),
                    provider.GetRequiredService<IMemoryCache>()
                );
            });

            applicationBuilder.Services.AddSingleton<ShipUpdater>(provider => new ShipUpdater(
                provider.GetRequiredService<MediaWikiClient>(),
                provider.GetRequiredService<ApiManager>(),
                DryRun));

            bool turrets = handler.ParseResult.GetValueForOption(turretsOption);

            if (turrets)
                applicationBuilder.Services.AddSingleton<TurretUpdater>(provider => new TurretUpdater(
                    provider.GetRequiredService<MediaWikiClient>(),
                    provider.GetRequiredService<ApiManager>()));

            IHost app = applicationBuilder.Build();

            if (await app.Services.GetRequiredService<MediaWikiClient>().IsLoggedIn())
                Log.Information("Logged in to MediaWiki");
            else
                Log.Error("Using MediaWiki anonymously. Editing will not be possible.");

            #endregion

            #region Sentry
#if !DEBUG
            SentrySdk.Init(options =>
            {
                options.Dsn = applicationBuilder.Configuration["SENTRY_DSN"] ?? "";
                options.AutoSessionTracking = true;
                options.TracesSampleRate = 1.0;
                options.ProfilesSampleRate = 1.0;
            });
#endif
            #endregion

            #region Scheduling Logic

            // TODO: Probably should refactor this logic. We kinda repeat ourselves here. It'd be better to move the job
            // creation logic outside of the if statements, and instead use the if statements for defining triggers. For
            // example, if the user doesn't specify a cron schedule, we can still use the Job to run the methods via
            // StartNow. Like a fire & forget. So we should probably remove the logic at the end of this method that
            // runs the one-off logic, and instead use the jobs for one-off logic. Though, this might not be possible
            // for running for individual ships.

            string? shipScheduleOptionValue = handler.ParseResult.GetValueForOption(shipScheduleOption);
            string? turretScheduleOptionValue = handler.ParseResult.GetValueForOption(turretScheduleOption);

            if (shipScheduleOptionValue != null || turretScheduleOptionValue != null)
            {
                IScheduler scheduler = await new StdSchedulerFactory().GetScheduler();
                await scheduler.Start();

                if (shipScheduleOptionValue != null)
                {
                    IJobDetail massUpdateJob = JobBuilder.Create<MassUpdateJob>()
                        .WithIdentity("massUpdateJob", "group1")
                        .Build();

                    massUpdateJob.JobDataMap.Put("shipUpdater", app.Services.GetRequiredService<ShipUpdater>());
                    massUpdateJob.JobDataMap.Put("healthCheckUrl", applicationBuilder.Configuration["HEALTHCHECK_URL"]);

                    ITrigger massUpdateTrigger = TriggerBuilder.Create()
                        .WithIdentity("massUpdateTrigger", "group1")
                        .ForJob("massUpdateJob", "group1")
                        .StartNow()
                        .WithCronSchedule(shipScheduleOptionValue)
                        .Build();

                    await scheduler.ScheduleJob(massUpdateJob, massUpdateTrigger);
                    Console.WriteLine(
                        $"Scheduled ship mass update job for {massUpdateTrigger.GetNextFireTimeUtc()?.ToLocalTime()}");
                    Console.WriteLine("Running a mass update job now...");
                    await scheduler.TriggerJob(new JobKey("massUpdateJob", "group1"));
                }

                if (turretScheduleOptionValue != null)
                {
                    IJobDetail turretUpdateJob = JobBuilder.Create<TurretUpdateJob>()
                        .WithIdentity("turretUpdateJob", "group1")
                        .Build();

                    turretUpdateJob.JobDataMap.Put("turretUpdater",
                        new TurretUpdater(app.Services.GetRequiredService<MediaWikiClient>(),
                            app.Services.GetRequiredService<ApiManager>()));

                    ITrigger turretUpdateTrigger = TriggerBuilder.Create()
                        .WithIdentity("turretUpdateTrigger", "group1")
                        .ForJob("turretUpdateJob", "group1")
                        .StartNow()
                        .WithCronSchedule(turretScheduleOptionValue)
                        .Build();

                    await scheduler.ScheduleJob(turretUpdateJob, turretUpdateTrigger);
                    Console.WriteLine("Scheduled turret update job");
                }

                await app.RunAsync();
            }

            #endregion

            #region Ship Option Handler

            string[] shipsOptionValue = handler.ParseResult.GetValueForOption(shipsOption)!;
            if (!shipsOptionValue.First().Equals("none", StringComparison.CurrentCultureIgnoreCase) &&
                shipScheduleOptionValue == null)
            {
                if (shipsOptionValue.First().Equals("all", StringComparison.CurrentCultureIgnoreCase))
                    await app.Services.GetRequiredService<ShipUpdater>().UpdateAllShips();
                else
                    await app.Services.GetRequiredService<ShipUpdater>().MassUpdateShips(shipsOptionValue.ToList());
            }

            #endregion

            #region Turret Option Handler

            if (turrets && turretScheduleOptionValue == null)
                await app.Services.GetRequiredService<TurretUpdater>().UpdateTurrets();

            #endregion
        });

        return await rootCommand.InvokeAsync(args);
    }
}