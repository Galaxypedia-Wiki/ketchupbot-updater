using System.Text.RegularExpressions;
using ketchupbot_updater.API;
using ketchupbot_updater.Types;

namespace ketchupbot_updater;

public class TurretUpdater(MwClient mwClient, ApiManager apiManager)
{
    public async Task UpdateTurrets(Dictionary<string, TurretData>? turretData = null)
    {
        turretData ??= await apiManager.GetTurretData();

        if (turretData == null) throw new Exception("Failed to fetch turret data");

        string turretPageWikitext = await mwClient.GetArticle("Turrets");

        MatchCollection turretTables = WikiParser.ExtractTurretTables(turretPageWikitext);
        string newTurretPageWikitext = turretPageWikitext;

        foreach ((Match? value, int index) in turretTables.Select((value, i) => (value, i)))
        {
            string[] tableSplit = value.Value.Split("|-");

            IEnumerable<KeyValuePair<string, TurretData>> relevantTurrets = turretData.Where(kvp =>
            {
                TurretData data = kvp.Value;
                return index switch
                {
                    0 => data.TurretType == TurretTypeEnum.Mining,
                    1 => data.TurretType == TurretTypeEnum.Laser,
                    2 => data.TurretType == TurretTypeEnum.Railgun,
                    3 => data.TurretType == TurretTypeEnum.Flak,
                    4 => data.TurretType == TurretTypeEnum.Cannon,
                    5 => data.TurretType == TurretTypeEnum.Pdl,
                    _ => false
                };
            });

            IEnumerable<string> turretsParsed = relevantTurrets.Select(kvp =>
            {
                TurretData turret = kvp.Value;
                // TODO: Should probably find a way to automatically generate this using string interpolation. Not a fan of hardcoding.
                return
                    $"\n| {turret.Name}\n| {turret.Size}\n| {turret.BaseAccuracy:F4}\n| {turret.Damage}\n| {turret.Range}\n| {turret.Reload:F2}\n| {turret.SpeedDenominator}\n| {turret.Dps:F2}";
            });

            // This is a lot less hard coded, but a hell to read. I'll leave it like this for now.
            string newTable = $"{tableSplit[0].Trim()}\n|-\n{string.Join("\n|-", turretsParsed).Trim()}\n|}}";

            newTurretPageWikitext = Regex.Replace(turretPageWikitext, value.Value, newTable);
        }

        if (newTurretPageWikitext == turretPageWikitext)
            throw new Exception("Turrets page is up to date");

        await mwClient.EditArticle("Turrets", newTurretPageWikitext, "Updating turrets");
    }

    // TODO: Maybe in the future provide a way to update a single turret. This isn't really needed right now, so I'm not going to implement it yet. I'll just leave it here until it's needed.
    public void UpdateTurret(string turretName, TurretData turretData)
    {
        throw new NotImplementedException();
    }

}