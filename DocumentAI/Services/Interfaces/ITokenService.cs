using DocumentAI.Models;
using System.Security.Claims;

namespace DocumentAI.Services.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(ApplicationUser user);
    ClaimsPrincipal? ValidateToken(string token);
}