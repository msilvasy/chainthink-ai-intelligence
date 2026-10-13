using DocumentAI.Data;
using DocumentAI.DTOs.Requests;
using DocumentAI.DTOs.Responses;
using DocumentAI.Models;
using DocumentAI.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DocumentAI.Services;

public sealed class UserSettingsService : IUserSettingsService
{
    private readonly ApplicationDbContext _dbContext;

    public UserSettingsService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<QuerySettingsResponse> GetOrCreateQuerySettingsAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var settings = await _dbContext.UserQuerySettings
            .SingleOrDefaultAsync(item => item.UserId == userId, cancellationToken);

        if (settings is null)
        {
            settings = new UserQuerySettings { UserId = userId };
            _dbContext.UserQuerySettings.Add(settings);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return ToResponse(settings);
    }

    public async Task<QuerySettingsResponse> UpdateQuerySettingsAsync(
        string userId,
        UpdateQuerySettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        var settings = await _dbContext.UserQuerySettings
            .SingleOrDefaultAsync(item => item.UserId == userId, cancellationToken);

        if (settings is null)
        {
            settings = new UserQuerySettings { UserId = userId };
            _dbContext.UserQuerySettings.Add(settings);
        }

        settings.Mode = request.Mode.ToLowerInvariant();
        settings.TopK = request.TopK;
        settings.ChunkSize = request.ChunkSize;
        settings.ChunkOverlap = request.ChunkOverlap;
        settings.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToResponse(settings);
    }

    private static QuerySettingsResponse ToResponse(UserQuerySettings settings) => new()
    {
        Mode = settings.Mode,
        TopK = settings.TopK,
        ChunkSize = settings.ChunkSize,
        ChunkOverlap = settings.ChunkOverlap
    };
}
