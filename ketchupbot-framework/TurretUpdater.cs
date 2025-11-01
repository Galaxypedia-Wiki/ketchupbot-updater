using System.Text.RegularExpressions;
using System.Text;
using ketchupbot_framework.API;
using ketchupbot_framework.Types;

namespace ketchupbot_framework;

public class TurretUpdater(MediaWikiClient mediaWikiClient, ApiManager apiManager)
{
    public async Task UpdateTurrets(Dictionary<string, TurretData>? turretData = null)
    {
        turretData ??= await apiManager.GetTurretData();

        if (turretData == null) throw new Exception("Failed to fetch turret data");

        string turretPageWikitext = await mediaWikiClient.GetArticle("Turrets");

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
                    6 => data.TurretType == TurretTypeEnum.Beam,
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

            newTurretPageWikitext = Regex.Replace(newTurretPageWikitext, Regex.Escape(value.Value), newTable);
        }

        await UpdateTurretCargoData(turretData);

        if (newTurretPageWikitext == turretPageWikitext)
            throw new Exception("Turrets page is up to date");

        // await mediaWikiClient.EditArticle("Turrets", newTurretPageWikitext, "Updating turrets");
    }

    public async Task UpdateTurretCargoData(Dictionary<string, TurretData> turretData)
    {
        StringBuilder cargoBuilder = new StringBuilder();

        // Including the table declaration on the page currently causes an issue where miraheze never populates the cargo table.
        // cargoBuilder.Append("{{#cargo_declare:_table=TurretData|name=String|size=String|class=String|turretsize=String|dps=Float|mass=Float|distance=Float|damage=Float|reload=Float|beamsize=Float|override=Boolean|maxcycle=Integer|numbarrels=Integer|baseaccuracy=Float|accuracyindex=Float|rampingstrength=Float|speeddenominator=Float}}");

        foreach (var kvp in turretData)
        {
            TurretData turret = kvp.Value;

            string name = turret.Name ?? "How?";
            string size = turret.Size ?? "Unknown";
            string Class = turret.Class ?? "Unknown";
            string turretSize = turret.TurretSize ?? "Unknown";

            double dps = turret.Dps ?? 0.0;
            double mass = turret.Mass ?? 0.0;
            double range = turret.Range ?? 0.0;
            double damage = turret.Damage ?? 0.0;
            double reload = turret.Reload ?? 0.0;
            double beamSize = turret.BeamSize ?? 0.0;
            bool overrideValue = turret.Override ?? false;

            int maxCycle = turret.MaxCycle ?? 0;
            int numBarrels = turret.NumBarrels ?? 0;

            double baseAccuracy = turret.BaseAccuracy ?? 0.0;
            double accuracyIndex = turret.AccuracyIndex ?? 0.0;
            double rampingStrength = turret.RampingStrength ?? 0.0;
            double speedDenominator = turret.SpeedDenominator ?? 0.0;

            string cargoStore = $"{{{{#cargo_store:_table=TurretData|name={name}|size={size}|class={Class}|turretsize={turretSize}|dps={dps:G5}|mass={mass:G5}|distance={range:G5}|damage={damage:G5}|reload={reload:G5}|beamsize={beamSize:G5}|override={overrideValue}|maxcycle={maxCycle}|numbarrels={numBarrels}|baseaccuracy={baseAccuracy:G5}|accuracyindex={accuracyIndex:G5}|rampingstrength={rampingStrength:G5}|speeddenominator={speedDenominator:G5}}}}}";

            cargoBuilder.Append(cargoStore);
        }

        string newTurretDataWikitext = cargoBuilder.ToString();
        string TurretDataWikitext = await mediaWikiClient.GetArticle("Template:TurretData");
        
        if (newTurretDataWikitext == TurretDataWikitext)
            throw new Exception("Turretdata is up to date");
        await mediaWikiClient.EditArticle("Template:TurretData", newTurretDataWikitext, "Updating turretdata");
    }

    // TODO: Maybe in the future provide a way to update a single turret. This isn't really needed right now, so I'm not
    // going to implement it yet. I'll just leave it here until it's needed.
    public void UpdateTurret(string turretName, TurretData turretData)
    {
        throw new NotImplementedException();
    }
}