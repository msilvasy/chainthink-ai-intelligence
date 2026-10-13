namespace DocumentAI.Models;

public sealed record DocumentChunk
{
    public required string Id { get; init; }
    public required string DocumentId { get; init; }
    public required string DocumentName { get; init; }
    public required string Content { get; init; }
    public required int ChunkIndex { get; init; }
    public required int StartPosition { get; init; }
    public required int EndPosition { get; init; }
    public Dictionary<string, string> Metadata { get; init; } = new();
}

