namespace DocumentAI.Models;

public sealed class UserQuerySettings
{
    public required string UserId { get; set; }
    public string Mode { get; set; } = "balanced";
    public int TopK { get; set; } = 5;
    public int ChunkSize { get; set; } = 512;
    public int ChunkOverlap { get; set; } = 50;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
