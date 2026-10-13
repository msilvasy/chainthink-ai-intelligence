namespace DocumentAI.DTOs.Responses;

public sealed class ErrorResponse
{
    public required string Error { get; init; }
    public required string Message { get; init; }
    public string? Details { get; init; }
    public required DateTime Timestamp { get; init; }
    public required string TraceId { get; init; }
}

