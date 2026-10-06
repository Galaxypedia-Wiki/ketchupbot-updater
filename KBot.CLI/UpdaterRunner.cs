using KBot.Framework;
using KBot.Framework.API;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KBot.CLI;

public class UpdaterRunner(IServiceProvider services, ILogger<UpdaterRunner> logger)
{
    public async Task RunAsync(string[] ships, bool turrets)
    {
        if (await services.GetRequiredService<MediaWikiClient>().IsLoggedInAsync())
            logger.LogInformation("Logged in to MediaWiki");
        else
            logger.LogError("Using MediaWiki anonymously. Editing will not be possible.");

        #region Ship Option Handler

        if (!ships.First().Equals("none", StringComparison.CurrentCultureIgnoreCase))
        {
            if (ships.First().Equals("all", StringComparison.CurrentCultureIgnoreCase))
                await services.GetRequiredService<ShipUpdater>().UpdateAllShips();
            else
                await services.GetRequiredService<ShipUpdater>().MassUpdateShips(ships.ToList());
        }

        #endregion

        #region Turret Option Handler

        if (turrets)
            await services.GetRequiredService<TurretUpdater>().UpdateTurrets();

        #endregion
    }
}
