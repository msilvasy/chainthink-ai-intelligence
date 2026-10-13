using DocumentAI.Configuration;
using DocumentAI.Models;
using DocumentAI.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace DocumentAI.Services;

public sealed class HybridSearchService : IHybridSearchService
{
    private readonly IChromaService _chromaService;
    private readonly IBm25Service _bm25Service;
    private readonly IEmbeddingService _embeddingService;
    private readonly SearchSettings _settings;
    private readonly ILogger<HybridSearchService> _logger;

    public HybridSearchService(IChromaService chromaService, IBm25Service bm25Service, IEmbeddingService embeddingService, IOptions<SearchSettings> settings, ILogger<HybridSearchService> logger)
    {
        _chromaService = chromaService; _bm25Service = bm25Service; _embeddingService = embeddingService; _settings = settings.Value; _logger = logger;
    }

    public async Task<IReadOnlyList<RetrievedContext>> HybridSearchAsync(string userId, string query, int topK, string? documentId = null, IReadOnlyList<string>? documentIds = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("User ID is required", nameof(userId));
        if (string.IsNullOrWhiteSpace(query)) throw new ArgumentException("Query is required", nameof(query));
        if (topK < 1) return [];

        var mode = _settings.Mode.Trim().ToLowerInvariant();
        var finalTopK = Math.Min(topK, _settings.FinalTopK > 0 ? _settings.FinalTopK : topK);
        var vectorTopK = Math.Max(finalTopK, _settings.VectorTopK);
        var bm25TopK = Math.Max(finalTopK, _settings.Bm25TopK);

        if (mode == "bm25") return await _bm25Service.SearchAsync(userId, query, finalTopK, documentId, documentIds, cancellationToken);

        var embedding = await _embeddingService.GenerateEmbeddingAsync(query, cancellationToken);
        if (mode == "vector") return await _chromaService.QueryAsync(userId, embedding, finalTopK, documentId, documentIds, cancellationToken);
        if (mode != "hybrid") throw new InvalidOperationException($"Unsupported Search:Mode '{_settings.Mode}'. Use Vector, Bm25, or Hybrid.");

        var vectorTask = _chromaService.QueryAsync(userId, embedding, vectorTopK, documentId, documentIds, cancellationToken);
        var bm25Task = _bm25Service.SearchAsync(userId, query, bm25TopK, documentId, documentIds, cancellationToken);
        await Task.WhenAll(vectorTask, bm25Task);

        var vectorResults = await vectorTask;
        var bm25Results = await bm25Task;
        var fused = RrfFusion.Fuse(vectorResults, bm25Results, _settings.RrfK, finalTopK,
            (chunkId, vectorRank, bm25Rank, rrfScore) => _logger.LogDebug("Hybrid result. ChunkId={ChunkId}, VectorRank={VectorRank}, Bm25Rank={Bm25Rank}, RrfScore={RrfScore}", chunkId, vectorRank, bm25Rank, rrfScore));
        _logger.LogInformation("Hybrid search counts. Vector={VectorCount}, Bm25={Bm25Count}, Final={FinalCount}", vectorResults.Count, bm25Results.Count, fused.Count);
        return fused;
    }
}
