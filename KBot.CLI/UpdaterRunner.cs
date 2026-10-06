using KBot.Framework;
using KBot.Framework.API;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace KBot.CLI;

public static class UpdaterRunner
{
    public static async Task RunAsync(IServiceProvider services, string[] ships, bool turrets)
    {
        if (await services.GetRequiredService<MediaWikiClient>().IsLoggedInAsync())
            Log.Information("Logged in to MediaWiki");
        else
            Log.Error("Using MediaWiki anonymously. Editing will not be possible.");

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
