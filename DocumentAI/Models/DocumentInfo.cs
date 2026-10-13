namespace DocumentAI.Models;

public sealed class DocumentInfo
{
    public required string Id { get; init; }
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public required long FileSize { get; init; }
    public required DateTime UploadedAt { get; init; }
    public required int ChunkCount { get; init; }
    public required int CharacterCount { get; init; }
}

