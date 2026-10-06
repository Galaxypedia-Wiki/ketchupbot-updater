using System.Diagnostics;
using KBot.Framework;
using Spectre.Console;

namespace KBot.CLI;

internal static class ShipProgressDisplay
{
    public static async Task RunAsync(Func<Action<ShipUpdateProgress>, Task> run, bool verbose = false)
    {
        var elapsed = Stopwatch.StartNew();
        ShipUpdateProgress result = new(0, 0, 0, 0);
        if (!verbose && AnsiConsole.Profile.Capabilities.Interactive && !Console.IsOutputRedirected)
        {
            await AnsiConsole.Progress().AutoClear(true).StartAsync(async context =>
            {
                var task = context.AddTask("Updating ships", autoStart: false);
                await run(update =>
                {
                    result = update;
                    task.MaxValue = Math.Max(1, update.Total);
                    task.Value = update.Completed;
                    task.Description = $"Updating ships — Updated: {update.Updated}  Unchanged: {update.Unchanged}  Failed: {update.Failed}";
                    task.StartTask();
                });
            });
        }
        else
        {
            Console.WriteLine("Updating ships...");
            await run(update => result = update);
        }

        AnsiConsole.WriteLine($"Completed in {elapsed.Elapsed.TotalSeconds:F1}s — {result.Updated} updated, {result.Unchanged} unchanged, {result.Failed} failed");
        if (result.Failed > 0 && !verbose)
            AnsiConsole.WriteLine("Use -v to show individual failure details.");
    }
}
