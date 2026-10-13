namespace DocumentAI.Models;

public sealed class Bm25RebuildChunk
{
    public required string UserId { get; init; }
    public required DocumentChunk Chunk { get; init; }
}
