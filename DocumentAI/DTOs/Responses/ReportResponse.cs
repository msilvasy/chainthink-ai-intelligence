namespace DocumentAI.DTOs.Responses;

public sealed class ReportResponse
{
    public required string Topic { get; init; }
    public required ReportContent Report { get; init; }
    public required List<SourceReference> Sources { get; init; }
    public required DateTime GeneratedAt { get; init; }
    public required double ProcessingTimeMs { get; init; }
}

public sealed class ReportContent
{
    public required string ExecutiveSummary { get; init; }
    public required List<string> KeyFindings { get; init; }
    public required List<string> Risks { get; init; }
    public required List<string> Recommendations { get; init; }
}

