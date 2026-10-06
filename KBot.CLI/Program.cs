using System.CommandLine;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

namespace KBot.CLI;

/// <summary>
///     Entrypoint, configuration, and initialization for the updater component
/// </summary>
public class Program
{
    private static bool DryRun { get; set; }

    private static async Task<int> Main(string[] args)
    {
        #region Options

        var shipsOption = new Option<string[]>("--ships", "--ship", "-s")
        {
            DefaultValueFactory = _ => ["all"],
            Description = """List of ships to update. "all" to update all ships, "none" to not update ships.""",
            AllowMultipleArgumentsPerToken = true
        };

        var turretsOption = new Option<bool>("--turrets", "-t")
        {
            Description = "Update turrets?"
        };

        var dryRunOption = new Option<bool>("--dry-run")
        {
            Description = "Pass to enable dry run. Only works in production due to some stupid technical foresight. This takes precedence over the environment variable"
        };

        var threadCountOption = new Option<int>("--threads")
        {
            DefaultValueFactory = _ => 0,
            Description = "Number of threads to use when updating ships. Set to 1 for single threaded execution, 0 for automatic (let the the .NET runtime manage the thread count dynamically)."
        };

        var secretsDirectoryOption = new Option<string>("--secrets-directory")
        {
            DefaultValueFactory = _ => AppContext.BaseDirectory,
            Description = "Directory where the appsettings.json file is located. Defaults to the directory holding the executable."
        };

        var verboseOption = new Option<bool>("--verbose", "-v")
        {
            Description = "Enable verbose logging"
        };

        #endregion

        var rootCommand = new RootCommand("KetchupBot Updater Component")
        {
            shipsOption,
            turretsOption,
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

        rootCommand.SetAction(async (parseResult, cancellationToken) =>
        {
            await using (Stream? stream = Assembly.GetExecutingAssembly()
                             .GetManifestResourceStream("KBot.CLI.Assets.banner.txt"))
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

            HostApplicationBuilder applicationBuilder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
            {
                ContentRootPath = Path.GetFullPath(parseResult.GetValue(secretsDirectoryOption)!)
            });
            applicationBuilder.Configuration.Sources.Clear();
            applicationBuilder.Configuration.AddJsonFile("appsettings.json", true, false);

            string configPath = Path.Combine(applicationBuilder.Environment.ContentRootPath, "appsettings.json");
            if (!OperatingSystem.IsWindows() && File.Exists(configPath))
            {
                try
                {
                    if ((File.GetUnixFileMode(configPath) & UnixFileMode.OtherRead) != 0)
                        Log.Warning("Configuration file {ConfigPath} is readable by everyone and may expose credentials. " +
                                    "Consider chmod 600 if the file is owned by the account running the application", configPath);
                }
                catch (IOException e)
                {
                    Log.Warning(e, "Could not check permissions for configuration file {ConfigPath}", configPath);
                }
                catch (UnauthorizedAccessException e)
                {
                    Log.Warning(e, "Could not check permissions for configuration file {ConfigPath}", configPath);
                }
            }

            if (applicationBuilder.Environment.IsDevelopment())
                applicationBuilder.Configuration.AddUserSecrets<Program>();

            applicationBuilder.Configuration.AddEnvironmentVariables();

            foreach (string key in new[] { "GIAPI_URL", "MWUSERNAME", "MWPASSWORD" })
                if (string.IsNullOrWhiteSpace(applicationBuilder.Configuration[key]))
                    throw new InvalidOperationException($"{key} not set");

            #region Configuration

#if DEBUG
            Log.Information("Running in development mode");
            levelSwitch.MinimumLevel = LogEventLevel.Debug;
            DryRun = true;
#else
            DryRun = parseResult.GetValue(dryRunOption);
            if (DryRun) Log.Information("Running in dry run mode");
#endif

            if (parseResult.GetValue(verboseOption))
            {
                levelSwitch.MinimumLevel = LogEventLevel.Verbose;
                Log.Information("Enabled verbose logging");
            }

            #endregion

            bool turrets = parseResult.GetValue(turretsOption);
            applicationBuilder.Services.AddUpdaterServices(DryRun, turrets);

            IHost app = applicationBuilder.Build();
            await app.Services.GetRequiredService<UpdaterRunner>().RunAsync(parseResult.GetValue(shipsOption)!, turrets);
        });

        return await rootCommand.Parse(args).InvokeAsync();
    }
}
