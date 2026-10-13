namespace DocumentAI.DTOs.Responses;

public sealed class QueryResponse
{
    public required string Answer { get; init; }
    public required List<SourceReference> Sources { get; init; }
    public required double ProcessingTimeMs { get; init; }
    public required bool AnswerFoundInContext { get; init; }
}

public sealed class SourceReference
{
    public required string DocumentId { get; init; }
    public required string DocumentName { get; init; }
    public required string ChunkId { get; init; }
    public required string Excerpt { get; init; }
    public required float RelevanceScore { get; init; }
}

