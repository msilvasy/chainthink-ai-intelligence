using DocumentAI.DTOs.Requests;
using DocumentAI.DTOs.Responses;

namespace DocumentAI.Services.Interfaces;

public interface IUserSettingsService
{
    Task<QuerySettingsResponse> GetOrCreateQuerySettingsAsync(string userId, CancellationToken cancellationToken = default);
    Task<QuerySettingsResponse> UpdateQuerySettingsAsync(string userId, UpdateQuerySettingsRequest request, CancellationToken cancellationToken = default);
}
