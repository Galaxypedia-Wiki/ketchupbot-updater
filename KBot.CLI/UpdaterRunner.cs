using KBot.Framework;
using KBot.Framework.API;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KBot.CLI;

public class UpdaterRunner(IServiceProvider services, ILogger<UpdaterRunner> logger)
{
    public async Task RunAsync(string[] ships, bool turrets, bool verbose = false)
    {
        if (await services.GetRequiredService<MediaWikiClient>().IsLoggedInAsync())
            logger.LogInformation("Logged in to MediaWiki");
        else
            logger.LogError("Using MediaWiki anonymously. Editing will not be possible.");

        #region Ship Option Handler

        if (!ships.First().Equals("none", StringComparison.CurrentCultureIgnoreCase))
        {
            ShipUpdater updater = services.GetRequiredService<ShipUpdater>();
            await ShipProgressDisplay.RunAsync(progress =>
                ships.First().Equals("all", StringComparison.CurrentCultureIgnoreCase)
                    ? updater.UpdateAllShips(progress: progress)
                    : updater.MassUpdateShips(ships.ToList(), progress: progress), verbose);
        }

        #endregion

        #region Turret Option Handler

        if (turrets)
            await services.GetRequiredService<TurretUpdater>().UpdateTurrets();

        #endregion
    }
}
