namespace DocumentAI.Scheduler;
public class FiveMinuteTimerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<FiveMinuteTimerService> _logger;
    public FiveMinuteTimerService(
        IServiceScopeFactory scopeFactory,
        ILogger<FiveMinuteTimerService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Five-minute timer service started.");
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
        while (!stoppingToken.IsCancellationRequested &&
               await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var job = scope.ServiceProvider.GetRequiredService < IMyScheduledJob>();
                await job.RunAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Normal shutdown.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while running the five-minute scheduled job.");
            }
        }
        _logger.LogInformation("Five-minute timer service stopped.");
    }
}
