namespace DocumentAI.Models;

public sealed class QueryLog
{
    public long Id { get; set; }
    public required string UserId { get; set; }
    public required string Query { get; set; }
    public required string Response { get; set; }
    public float? RelevanceScore { get; set; }
    public double ProcessingTimeMs { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
