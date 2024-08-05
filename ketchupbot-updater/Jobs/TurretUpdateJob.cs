using Quartz;

namespace ketchupbot_updater.Jobs;

public class TurretUpdateJob : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        JobDataMap jobDataMap = context.MergedJobDataMap;

        var turretUpdater = (TurretUpdater)jobDataMap["turretUpdater"];
        if (turretUpdater == null) throw new InvalidOperationException("TurretUpdater not found in job data map");

        await turretUpdater.UpdateTurrets();
    }
}