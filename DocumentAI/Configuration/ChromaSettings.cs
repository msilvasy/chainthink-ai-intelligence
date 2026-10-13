namespace DocumentAI.Configuration;

public sealed class ChromaSettings
{
    public const string SectionName = "Chroma";
    
    public string BaseUrl { get; set; } = "http://localhost:8000";
    public string CollectionName { get; set; } = "documents";
    public int TopK { get; set; } = 5;
    public int TimeoutSeconds { get; set; } = 60;
    public string DistanceMetric { get; set; } = "cosine";
}

