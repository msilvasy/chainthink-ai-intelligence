using DocumentAI.Scheduler;

public class MyScheduledJob : IMyScheduledJob
{
    private readonly ILogger<MyScheduledJob> _logger;
    public MyScheduledJob(ILogger<MyScheduledJob> logger)
    {
        _logger = logger;
    }
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Scheduled job started at {Time}", DateTimeOffset.Now);
        // Put the logic you want to run every 5 minutes here.
        await Task.Delay(100, cancellationToken);
        _logger.LogInformation("Scheduled job completed at {Time}", DateTimeOffset.Now);
    }
}
