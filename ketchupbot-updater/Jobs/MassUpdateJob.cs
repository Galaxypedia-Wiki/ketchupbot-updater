using Quartz;

namespace ketchupbot_updater.Jobs;

public class MassUpdateJob : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        JobDataMap jobDataMap = context.MergedJobDataMap;

        var shipUpdater = (ShipUpdater)jobDataMap["shipUpdater"];


    }
}