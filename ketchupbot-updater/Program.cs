using System.CommandLine;
using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using ketchupbot_updater.API;
using ketchupbot_updater.Types;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
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
            "List of ships to update. \"all\" to update all ships, \"none\" to not update ships"
        )
        {
            AllowMultipleArgumentsPerToken = true
        };

        var turretsOption = new Option<bool>(
            ["-t", "--turrets"],
            "Update turrets?"
        );

        var usernameOption = new Option<string>(
            ["-mu", "--username"],
            "Username for logging into the wiki. This takes precedence over the environment variable"
        );

        var passwordOption = new Option<string>(
            ["-mp", "--password"],
            "Password for logging into the wiki. This takes precedence over the environment variable"
        );

        var galaxyInfoApiUrlOption = new Option<string>(
            ["-ga", "--galaxy-info-api"],
            "Galaxy Info API URL. This takes precedence over the environment variable"
        );

        var galaxyInfoApiTokenOption = new Option<string>(
            ["-gt", "--galaxy-info-token"],
            "Galaxy Info API Token. This takes precedence over the environment variable"
        );

        var shipScheduleOption = new Option<string>(
            ["-ss", "--ship-schedule"],
            "Pass to enable the ship schduler. Will ignore ships option. This takes precedence over the environment variable"
        )
        {
            IsRequired = false
        };

        var turretScheduleOption = new Option<string>(
            ["-ts", "--turret-schedule"],
            "Pass to enable the turret schduler. Will ignore turrets option. This takes precedence over the environment variable"
        )
        {
            IsRequired = false
        };

        var dryRunOption = new Option<bool>(
            "--dry-run",
            "Pass to enable dry run. This takes precedence over the environment variable"
        );

        #endregion

        var rootCommand = new RootCommand("KetchupBot Updater Component")
        {
            shipsOption,
            turretsOption,
            usernameOption,
            passwordOption,
            galaxyInfoApiUrlOption,
            galaxyInfoApiTokenOption,
            shipScheduleOption,
            turretScheduleOption,
            dryRunOption
        };

        await using (Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ketchupbot_updater.Assets.banner.txt"))
        {
            if (stream == null)
                throw new Exception("Failed to load banner");

            using (var reader = new StreamReader(stream)) Console.WriteLine(reader.ReadToEnd());
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
#else
        Console.WriteLine("Running in development mode");
        DryRun = true;
#endif

        rootCommand.SetHandler(idk => DryRun = configuration["DRY_RUN"] == "true" || idk,
            dryRunOption);
        #endregion

        // var schedulerFactory = new StdSchedulerFactory();
        // IScheduler scheduler = schedulerFactory.GetScheduler().GetAwaiter().GetResult();


        var mwClient = new MwClient(configuration["MWUSERNAME"] ?? throw new InvalidOperationException(), configuration["MWPASSWORD"] ?? throw new InvalidOperationException());
        Logger.Log("Logged into the wiki", style: LogStyle.Checkmark);
        var apiManager = new ApiManager("https://api.info.galaxy.casa", configuration["GIAPI_TOKEN"] ?? throw new InvalidOperationException());
        var shipUpdater = new ShipUpdater(mwClient, apiManager);

        rootCommand.SetHandler(async ships =>
        {
            if (ships.First() == "all")
                await shipUpdater.UpdateAllShips();
            else if (ships.First() != "none")
            {
                foreach (string ship in ships)
                {
                    await shipUpdater.UpdateShip(ship);
                }
            }
        }, shipsOption);


        // Dictionary<string, ShipData>? fart = apiManager.GetShipsData().GetAwaiter().GetResult();
        //
        // if (fart != null)
        //     Console.WriteLine(JsonConvert.SerializeObject(fart.First(), Formatting.Indented, new JsonSerializerSettings
        //     {
        //         NullValueHandling = NullValueHandling.Ignore
        //     }));

        return await rootCommand.InvokeAsync(args);
    }
}