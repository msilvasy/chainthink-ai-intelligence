using DocumentAI.DTOs.Requests;
using DocumentAI.DTOs.Responses;
using DocumentAI.Models;
using DocumentAI.Services.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace DocumentAI.Services;

public sealed class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserSettingsService _userSettingsService;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        IUserSettingsService userSettingsService,
        ITokenService tokenService,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _userSettingsService = userSettingsService;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if user already exists
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "User with this email already exists."
                };
            }

            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogWarning("User registration failed for {Email}: {Errors}", request.Email, errors);
                return new AuthResponse
                {
                    Success = false,
                    Message = $"Registration failed: {errors}"
                };
            }

            var token = _tokenService.GenerateAccessToken(user);
            await _userManager.UpdateAsync(user);
            var querySettings = await _userSettingsService.GetOrCreateQuerySettingsAsync(user.Id, cancellationToken);

            _logger.LogInformation("User registered successfully: {Email}", request.Email);

            return new AuthResponse
            {
                Success = true,
                Message = "User registered successfully.",
                Token = token,
                Email = user.Email,
                UserId = user.Id,
                QuerySettings = querySettings
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during user registration");
            return new AuthResponse
            {
                Success = false,
                Message = "An error occurred during registration."
            };
        }
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                _logger.LogWarning("Login attempt for non-existent email: {Email}", request.Email);
                return new AuthResponse
                {
                    Success = false,
                    Message = "Invalid email or password."
                };
            }

            var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
            if (!passwordValid)
            {
                _logger.LogWarning("Invalid password attempt for email: {Email}", request.Email);
                return new AuthResponse
                {
                    Success = false,
                    Message = "Invalid email or password."
                };
            }

            var token = _tokenService.GenerateAccessToken(user);
            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);
            var querySettings = await _userSettingsService.GetOrCreateQuerySettingsAsync(user.Id, cancellationToken);

            _logger.LogInformation("User logged in successfully: {Email}", request.Email);

            return new AuthResponse
            {
                Success = true,
                Message = "Login successful.",
                Token = token,
                Email = user.Email,
                UserId = user.Id,
                FirstName=user.FirstName,
                LastName=user.LastName,
                QuerySettings = querySettings
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during login");
            return new AuthResponse
            {
                Success = false,
                Message = "An error occurred during login."
            };
        }
    }

}
