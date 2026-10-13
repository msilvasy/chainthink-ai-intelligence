namespace DocumentAI.Scheduler
{
    public interface IMyScheduledJob
    {
        Task RunAsync(CancellationToken cancellationToken);
    }
}
