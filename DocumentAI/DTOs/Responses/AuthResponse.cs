namespace DocumentAI.DTOs.Responses;

public sealed class AuthResponse
{
    public bool Success { get; set; }
    public required string Message { get; set; }
    public string? Token { get; set; }
    public string? Email { get; set; }
    public string? UserId { get; set; }
    public string? LastName { get; internal set; }
    public string? FirstName { get; internal set; }
    public QuerySettingsResponse? QuerySettings { get; set; }
}
