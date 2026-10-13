namespace DocumentAI.Configuration;

public sealed class OllamaSettings
{
    public const string SectionName = "Ollama";
    
    public string BaseUrl { get; set; } = "http://localhost:11434";
    public string ChatModel { get; set; } = "llama3.1:8b";
    public string EmbeddingModel { get; set; } = "nomic-embed-text";
    public int TimeoutSeconds { get; set; } = 300;
    public int MaxTokens { get; set; } = 4096;
    public float Temperature { get; set; } = 0.1f;
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrlCloud { get; set; } = "https://ollama.com";

}

