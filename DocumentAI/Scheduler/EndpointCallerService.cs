using DocumentAI.Models;
using DocumentAI.Services;
using DocumentAI.Services.Interfaces;
using System.Text.Json;

public class EndpointCallerService : BackgroundService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<EndpointCallerService> _logger;
    private readonly IEmailService _emailService;

    public EndpointCallerService(
        IHttpClientFactory httpClientFactory, IEmailService emailService,
        ILogger<EndpointCallerService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _emailService = emailService;

    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));

        while (!stoppingToken.IsCancellationRequested &&
               await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                var client = _httpClientFactory.CreateClient("LocalApi");

                //var response = await client.PostAsync("https://chainthink.cloud/api/jobs/run", null, stoppingToken);
                var healthResponse = await client.GetAsync("https://chainthink.cloud/api/documents/health", stoppingToken);

                if (healthResponse.IsSuccessStatusCode)
                {
                    string responseBody = await healthResponse.Content.ReadAsStringAsync();
                    var health = JsonSerializer.Deserialize<HealthResponse>(
                                  responseBody,
                                  new JsonSerializerOptions
                                  {
                                      PropertyNameCaseInsensitive = true
                                  });

                    if (health?.Status == "Degraded")
                    {
                        var unhealthyServices = new List<string>();

                        if (health.Services.ChromaDb == "Unhealthy")
                            unhealthyServices.Add("ChromaDB");

                        if (health.Services.Ollama == "Unhealthy")
                            unhealthyServices.Add("Ollama");

                        await _emailService.SendAsync(
                                                      "msilvasy@aol.com",
                                                      "AI Services Health Alert",
                                                      responseBody);
                        _logger.LogInformation("Endpoint called successfully at {Time}", DateTimeOffset.Now);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Endpoint call failed with status code {StatusCode}",
                            healthResponse.StatusCode);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Normal shutdown.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while calling endpoint from timer.");
            }
        }
    }
}