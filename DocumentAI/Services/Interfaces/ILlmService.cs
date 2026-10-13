using DocumentAI.DTOs.Responses;
using DocumentAI.Models;

namespace DocumentAI.Services.Interfaces;

public interface ILlmService
{
    Task<(string Answer, bool FoundInContext)> AnswerQueryAsync(string modelId,
        string query, 
        IReadOnlyList<RetrievedContext> contexts, 
        CancellationToken cancellationToken = default);
    
    Task<ReportContent> GenerateReportAsync(string modelId,
        string topic, 
        IReadOnlyList<RetrievedContext> contexts, 
        string? additionalInstructions = null,
        CancellationToken cancellationToken = default);
    
    Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default);

    Task<string> GetModelsAsync(CancellationToken cancellationToken = default);
}

