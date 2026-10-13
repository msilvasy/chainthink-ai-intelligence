using DocumentAI.DTOs.Responses;

namespace DocumentAI.Services.Interfaces;

public interface IQueryLogService
{
    Task CreateAsync(
        string userId,
        string query,
        string response,
        float? relevanceScore,
        double processingTimeMs,
        CancellationToken cancellationToken = default);

    Task<List<QueryLogResponse>> GetAsync(string? userId, CancellationToken cancellationToken = default);
}
