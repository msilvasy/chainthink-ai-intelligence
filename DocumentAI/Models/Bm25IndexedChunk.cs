namespace DocumentAI.Models;

public sealed class Bm25IndexedChunk
{
    public required string ChunkId { get; set; }
    public required string UserId { get; set; }
    public required string DocumentId { get; set; }
    public required string DocumentName { get; set; }
    public required string Content { get; set; }
    public required string MetadataJson { get; set; }
    public DateTime IndexedAt { get; set; } = DateTime.UtcNow;
}
