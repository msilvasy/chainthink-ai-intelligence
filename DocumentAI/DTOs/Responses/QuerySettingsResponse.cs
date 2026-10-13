namespace DocumentAI.DTOs.Responses;

public sealed class QuerySettingsResponse
{
    public required string Mode { get; init; }
    public int TopK { get; init; }
    public int ChunkSize { get; init; }
    public int ChunkOverlap { get; init; }
}
