using DocumentAI.Data;
using DocumentAI.DTOs.Responses;
using DocumentAI.Models;
using DocumentAI.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DocumentAI.Services;

public sealed class QueryLogService : IQueryLogService
{
    private readonly ApplicationDbContext _dbContext;

    public QueryLogService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task CreateAsync(
        string userId,
        string query,
        string response,
        float? relevanceScore,
        double processingTimeMs,
        CancellationToken cancellationToken = default)
    {
        _dbContext.QueryLogs.Add(new QueryLog
        {
            UserId = userId,
            Query = query,
            Response = response,
            RelevanceScore = relevanceScore,
            ProcessingTimeMs = processingTimeMs
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<QueryLogResponse>> GetAsync(string? userId, CancellationToken cancellationToken = default)
    {
        var logs = _dbContext.QueryLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(userId))
        {
            logs = logs.Where(log => log.UserId == userId);
        }

        return await logs
            .OrderByDescending(log => log.CreatedAt)
            .Select(log => new QueryLogResponse
            {
                Id = log.Id,
                UserId = log.UserId,
                Query = log.Query,
                Response = log.Response,
                RelevanceScore = log.RelevanceScore,
                ProcessingTimeMs = log.ProcessingTimeMs,
                CreatedAt = log.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }
}
