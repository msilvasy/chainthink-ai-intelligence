namespace DocumentAI.DTOs.Responses;

public sealed class QueryLogResponse
{
    public long Id { get; init; }
    public required string UserId { get; init; }
    public required string Query { get; init; }
    public required string Response { get; init; }
    public float? RelevanceScore { get; init; }
    public double ProcessingTimeMs { get; init; }
    public DateTime CreatedAt { get; init; }
}
