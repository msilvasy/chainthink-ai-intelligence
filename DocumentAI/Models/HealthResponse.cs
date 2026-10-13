namespace DocumentAI.Models
{
    using System.Text.Json.Serialization;

    public class HealthResponse
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("services")]
        public ServiceStatus Services { get; set; } = new();
    }

    public class ServiceStatus
    {
        [JsonPropertyName("chromaDb")]
        public string ChromaDb { get; set; } = string.Empty;

        [JsonPropertyName("ollama")]
        public string Ollama { get; set; } = string.Empty;
    }
}
