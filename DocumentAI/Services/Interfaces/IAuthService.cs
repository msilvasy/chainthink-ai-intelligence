using DocumentAI.DTOs.Requests;
using DocumentAI.DTOs.Responses;

namespace DocumentAI.Services.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

}