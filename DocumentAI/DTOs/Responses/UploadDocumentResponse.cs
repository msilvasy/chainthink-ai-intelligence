namespace DocumentAI.DTOs.Responses;

public sealed class UploadDocumentResponse
{
    public required string DocumentId { get; init; }
    public required string FileName { get; init; }
    public required int ChunkCount { get; init; }
    public required int CharacterCount { get; init; }
    public required DateTime ProcessedAt { get; init; }
    public required string Message { get; init; }
}

