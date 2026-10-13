namespace DocumentAI.DTOs.Responses;

public sealed class EmbeddedDocumentResponse
{
    public required string UserId { get; set; }
    public required string DocumentId { get; init; }
    public required string DocumentName { get; init; }
    public required int ChunkCount { get; init; }
    public required DateTime UploadedAt { get; init; }
}

