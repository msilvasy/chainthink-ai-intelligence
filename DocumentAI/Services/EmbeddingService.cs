using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using DocumentAI.Configuration;
using DocumentAI.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace DocumentAI.Services;

public sealed class EmbeddingService : IEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly OllamaSettings _settings;
    private readonly ILogger<EmbeddingService> _logger;
    private int? _cachedDimension;

    public EmbeddingService(
        HttpClient httpClient, 
        IOptions<OllamaSettings> settings,
        ILogger<EmbeddingService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Text cannot be empty", nameof(text));

        var request = new OllamaEmbeddingRequest
        {
            Model = _settings.EmbeddingModel,
            Prompt = text
        };

        var response = await _httpClient.PostAsJsonAsync(
            $"{_settings.BaseUrl}/api/embeddings",
            request,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OllamaEmbeddingResponse>(cancellationToken);
        
        if (result?.Embedding == null || result.Embedding.Length == 0)
        {
            throw new InvalidOperationException("Failed to generate embedding: empty response from Ollama");
        }

        _cachedDimension ??= result.Embedding.Length;
        
        _logger.LogDebug("Generated embedding with {Dimension} dimensions", result.Embedding.Length);
        return result.Embedding;
    }

    public async Task<float[][]> GenerateEmbeddingsAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken = default)
    {
        if (texts == null || texts.Count == 0)
            throw new ArgumentException("Texts cannot be empty", nameof(texts));

        _logger.LogInformation("Generating embeddings for {Count} texts", texts.Count);

        var embeddings = new float[texts.Count][];
        
        // Process in batches to avoid overwhelming the server
        const int batchSize = 10;
        
        for (int i = 0; i < texts.Count; i += batchSize)
        {
            var batchTasks = new List<Task<float[]>>();
            var batchEnd = Math.Min(i + batchSize, texts.Count);
            
            for (int j = i; j < batchEnd; j++)
            {
                var index = j;
                batchTasks.Add(GenerateEmbeddingAsync(texts[index], cancellationToken));
            }

            var batchResults = await Task.WhenAll(batchTasks);
            
            for (int j = 0; j < batchResults.Length; j++)
            {
                embeddings[i + j] = batchResults[j];
            }

            _logger.LogDebug("Processed batch {BatchStart}-{BatchEnd} of {Total}", i, batchEnd - 1, texts.Count);
        }

        return embeddings;
    }

    public int GetEmbeddingDimension()
    {
        // nomic-embed-text produces 768-dimensional embeddings
        return _cachedDimension ?? 768;
    }

    private sealed class OllamaEmbeddingRequest
    {
        [JsonPropertyName("model")]
        public required string Model { get; init; }

        [JsonPropertyName("prompt")]
        public required string Prompt { get; init; }
    }

    private sealed class OllamaEmbeddingResponse
    {
        [JsonPropertyName("embedding")]
        public float[]? Embedding { get; init; }
    }
}

