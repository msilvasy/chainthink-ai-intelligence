using DocumentAI.Scheduler;
using Microsoft.AspNetCore.Mvc;
[ApiController]
[Route("api/[controller]")]
public class JobsController : ControllerBase
{
    private readonly IMyScheduledJob _job;
    public JobsController(IMyScheduledJob job)
    {
        _job = job;
    }
    [HttpPost("run")]
    public async Task<IActionResult> Run(CancellationToken cancellationToken)
    {
        await _job.RunAsync(cancellationToken);
        return Ok(new { message = "Job executed successfully." });
    }
}
