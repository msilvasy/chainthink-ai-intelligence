namespace DocumentAI.Configuration;

public sealed class SearchSettings
{
    public const string SectionName = "Search";

    public string Mode { get; set; } = "Hybrid";
    public int VectorTopK { get; set; } = 10;
    public int Bm25TopK { get; set; } = 10;
    public int FinalTopK { get; set; } = 5;
    public int RrfK { get; set; } = 60;
    public double Bm25K1 { get; set; } = 1.2;
    public double Bm25B { get; set; } = 0.75;
}
