namespace DocumentAI.DTOs.Responses;

public sealed class EmbeddedChunkResponse
{
    public required string ChunkId { get; init; }
    public required string PreviewText { get; init; }
    public required int EmbeddingVectorSize { get; init; }
}

