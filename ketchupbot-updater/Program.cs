using System.CommandLine;
using System.Reflection;
using ketchupbot_updater.API;
using ketchupbot_updater.Jobs;
using Microsoft.Extensions.Configuration;
using Quartz;
using Quartz.Impl;

namespace ketchupbot_updater;

/// <summary>
/// Entrypoint, configuration, and initialization for the updater component
/// </summary>
public static class Program
{
    public static bool DryRun { get; private set; }

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
            "Pass a schedule (in cron format) to enable the ship scheduler. Can pass Will ignore --ships option. This takes precedence over the environment variable"
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
            getDefaultValue: () => 0,
            "Number of threads to use when updating ships. Set to 1 for singlethreaded, 0 for automatic (let the the .NET runtime decide the thread count)");

        #endregion

        var rootCommand = new RootCommand("KetchupBot Updater Component")
        {
            shipsOption,
            turretsOption,
            shipScheduleOption,
            turretScheduleOption,
            dryRunOption,
            threadCountOption
        };

        rootCommand.SetHandler(async handler =>
        {
            await using (Stream? stream = Assembly.GetExecutingAssembly()
                             .GetManifestResourceStream("ketchupbot_updater.Assets.banner.txt"))
            {
                if (stream == null)
                    throw new Exception("Failed to load banner");

                using (var reader = new StreamReader(stream)) Console.WriteLine(await reader.ReadToEndAsync());
            }

            Console.WriteLine(
                $"\nketchupbot-updater | v{Assembly.GetExecutingAssembly().GetName().Version} | {DateTime.Now}\n");

            #region Configuration

            IConfigurationBuilder builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();

            IConfigurationRoot configuration = builder.Build();

#if !DEBUG
            Production = true;
            DryRun = handler.ParseResult.GetValueForOption(dryRunOption);
#else
            Console.WriteLine("Running in development mode");
            DryRun = true;
#endif

            #endregion

            var schedulerFactory = new StdSchedulerFactory();
            IScheduler scheduler = await schedulerFactory.GetScheduler();

            await scheduler.Start();

            var mwClient = new MwClient(configuration["MWUSERNAME"] ?? throw new InvalidOperationException(),
                configuration["MWPASSWORD"] ?? throw new InvalidOperationException());
            Logger.Log("Logged into the Galaxypedia", style: LogStyle.Checkmark);
            var apiManager = new ApiManager("https://api.info.galaxy.casa",
                configuration["GIAPI_TOKEN"] ?? throw new InvalidOperationException());

            string? shipScheduleOptionValue = handler.ParseResult.GetValueForOption(shipScheduleOption);
            string? turretScheduleOptionValue = handler.ParseResult.GetValueForOption(turretScheduleOption);

            if (shipScheduleOptionValue != null)
            {
                IJobDetail massUpdateJob = JobBuilder.Create<MassUpdateJob>()
                    .WithIdentity("massUpdateJob", "group1")
                    .Build();

                massUpdateJob.JobDataMap.Put("shipUpdater", new ShipUpdater(mwClient, apiManager));

                ITrigger massUpdateTrigger = TriggerBuilder.Create()
                    .WithIdentity("massUpdateTrigger", "group1")
                    .ForJob("massUpdateJob", "group1")
                    .StartNow()
                    .WithCronSchedule(shipScheduleOptionValue)
                    .Build();

                await scheduler.ScheduleJob(massUpdateJob, massUpdateTrigger);
            }

            #region Ship Option Handler

            string[]? shipsOptionValue = handler.ParseResult.GetValueForOption(shipsOption);
            if (shipsOptionValue != null && shipsOptionValue.First() != "none")
            {
                var shipUpdater = new ShipUpdater(mwClient, apiManager);

                if (shipsOptionValue.First() == "all")
                    await shipUpdater.UpdateAllShips();
                else
                {
                    foreach (string ship in shipsOptionValue)
                    {
                        await shipUpdater.UpdateShip(ship);
                    }
                }
            }

            #endregion

            #region Turret Option Handler

            bool turrets = handler.ParseResult.GetValueForOption(turretsOption);
            if (turrets) await new TurretUpdater(mwClient, apiManager).UpdateTurrets();

            #endregion
        });

        return await rootCommand.InvokeAsync(args);
    }
}