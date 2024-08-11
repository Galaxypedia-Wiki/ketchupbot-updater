using ketchupbot_framework;
using Quartz;

namespace ketchupbot_updater.Jobs;

public class MassUpdateJob : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        JobDataMap jobDataMap = context.MergedJobDataMap;

        var shipUpdater = (ShipUpdater)jobDataMap["shipUpdater"];
        if (shipUpdater == null) throw new InvalidOperationException("ShipUpdater not found in job data map");

        // I was thinking that maybe we should wrap this in a try/catch block. And we probably should, because I don't
        // think we'd be able to catch any errors further up in the call stack. Not sure though, so I'll leave it for
        // now until I make a decision.
        await shipUpdater.UpdateAllShips();

        Console.WriteLine("Mass update job completed");
        if (context.NextFireTimeUtc != null)
            Console.WriteLine("Next update is scheduled for " + context.NextFireTimeUtc.Value.LocalDateTime);
    }

}