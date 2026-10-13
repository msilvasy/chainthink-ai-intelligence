namespace DocumentAI.Configuration;

public sealed class ChunkingSettings
{
    public const string SectionName = "Chunking";
    
    public int ChunkSize { get; set; } = 512;
    public int ChunkOverlap { get; set; } = 50;
    public int MinChunkSize { get; set; } = 100;
}

