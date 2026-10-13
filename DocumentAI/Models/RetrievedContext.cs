namespace DocumentAI.Models;

public sealed class RetrievedContext
{
    public required string ChunkId { get; init; }
    public required string DocumentId { get; init; }
    public required string DocumentName { get; init; }
    public required string Content { get; init; }
    public float? Distance { get; init; }
    public float? Similarity { get; init; }
    public double? DisplayScore { get; init; }
    public Dictionary<string, string> Metadata { get; init; } = new();
}

