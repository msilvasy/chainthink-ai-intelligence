using System.Text.Json;
using System.Text.RegularExpressions;
using DocumentAI.Configuration;
using DocumentAI.Data;
using DocumentAI.Models;
using DocumentAI.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DocumentAI.Services;

public sealed partial class Bm25Service : IBm25Service
{
    private readonly ApplicationDbContext _dbContext;
    private readonly SearchSettings _settings;
    private readonly ILogger<Bm25Service> _logger;

    public Bm25Service(ApplicationDbContext dbContext, IOptions<SearchSettings> settings, ILogger<Bm25Service> logger)
    {
        _dbContext = dbContext;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task IndexChunksAsync(string userId, IReadOnlyList<DocumentChunk> chunks, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("User ID is required", nameof(userId));
        if (chunks.Count == 0) return;

        var ids = chunks.Select(chunk => chunk.Id).ToList();
        var existing = await _dbContext.Bm25IndexedChunks
            .Where(chunk => ids.Contains(chunk.ChunkId))
            .ToDictionaryAsync(chunk => chunk.ChunkId, cancellationToken);

        foreach (var chunk in chunks)
        {
            var indexed = ToIndexedChunk(userId, chunk);
            if (existing.TryGetValue(chunk.Id, out var current))
            {
                current.UserId = indexed.UserId;
                current.DocumentId = indexed.DocumentId;
                current.DocumentName = indexed.DocumentName;
                current.Content = indexed.Content;
                current.MetadataJson = indexed.MetadataJson;
                current.IndexedAt = indexed.IndexedAt;
            }
            else
            {
                _dbContext.Bm25IndexedChunks.Add(indexed);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Indexed {Count} chunks for BM25. UserId={UserId}", chunks.Count, userId);
    }

    public async Task<IReadOnlyList<RetrievedContext>> SearchAsync(string userId, string query, int topK, string? documentId = null, IReadOnlyList<string>? documentIds = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("User ID is required", nameof(userId));
        if (string.IsNullOrWhiteSpace(query) || topK < 1) return [];

        var chunks = _dbContext.Bm25IndexedChunks.AsNoTracking().Where(chunk => chunk.UserId == userId);
        if (documentIds is { Count: > 0 }) chunks = chunks.Where(chunk => documentIds.Contains(chunk.DocumentId));
        else if (!string.IsNullOrWhiteSpace(documentId)) chunks = chunks.Where(chunk => chunk.DocumentId == documentId);

        var candidates = await chunks.ToListAsync(cancellationToken);
        var queryTerms = Tokenize(query).Distinct(StringComparer.Ordinal).ToList();
        if (candidates.Count == 0 || queryTerms.Count == 0) return [];

        var documents = candidates.Select(chunk => new TokenizedChunk(chunk, Tokenize(chunk.Content).ToList())).ToList();
        var averageLength = documents.Average(document => Math.Max(document.Terms.Count, 1));
        var documentFrequency = queryTerms.ToDictionary(
            term => term,
            term => documents.Count(document => document.Terms.Contains(term)),
            StringComparer.Ordinal);

        var scored = documents
            .Select(document => new { document.Chunk, Score = CalculateScore(document.Terms, queryTerms, documentFrequency, documents.Count, averageLength) })
            .Where(result => result.Score > 0)
            .OrderByDescending(result => result.Score)
            .ThenBy(result => result.Chunk.ChunkId, StringComparer.Ordinal)
            .Take(topK)
            .Select(result => ToContext(result.Chunk))
            .ToList();

        _logger.LogInformation("BM25 returned {Count} results. UserId={UserId}", scored.Count, userId);
        return scored;
    }

    public async Task DeleteDocumentAsync(string documentId, CancellationToken cancellationToken = default)
    {
        var chunks = await _dbContext.Bm25IndexedChunks.Where(chunk => chunk.DocumentId == documentId).ToListAsync(cancellationToken);
        _dbContext.Bm25IndexedChunks.RemoveRange(chunks);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Deleted {Count} BM25 chunks for document {DocumentId}", chunks.Count, documentId);
    }

    public async Task DeleteChunkAsync(string chunkId, CancellationToken cancellationToken = default)
    {
        var chunk = await _dbContext.Bm25IndexedChunks.FindAsync([chunkId], cancellationToken);
        if (chunk is not null)
        {
            _dbContext.Bm25IndexedChunks.Remove(chunk);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        _logger.LogInformation("Deleted BM25 chunk {ChunkId}", chunkId);
    }

    public async Task RebuildAsync(IReadOnlyList<Bm25RebuildChunk> chunks, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        _dbContext.Bm25IndexedChunks.RemoveRange(_dbContext.Bm25IndexedChunks);
        await _dbContext.SaveChangesAsync(cancellationToken);

        foreach (var group in chunks.GroupBy(item => item.UserId, StringComparer.Ordinal))
        {
            await IndexChunksAsync(group.Key, group.Select(item => item.Chunk).ToList(), cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        _logger.LogInformation("Rebuilt BM25 index with {Count} chunks", chunks.Count);
    }

    private double CalculateScore(IReadOnlyList<string> documentTerms, IReadOnlyList<string> queryTerms, IReadOnlyDictionary<string, int> documentFrequency, int documentCount, double averageLength)
    {
        var score = 0d;
        foreach (var term in queryTerms)
        {
            var termFrequency = documentTerms.Count(token => token == term);
            if (termFrequency == 0) continue;
            var inverseDocumentFrequency = Math.Log(1 + (documentCount - documentFrequency[term] + 0.5) / (documentFrequency[term] + 0.5));
            var denominator = termFrequency + _settings.Bm25K1 * (1 - _settings.Bm25B + _settings.Bm25B * documentTerms.Count / averageLength);
            score += inverseDocumentFrequency * (termFrequency * (_settings.Bm25K1 + 1)) / denominator;
        }
        return score;
    }

    private static Bm25IndexedChunk ToIndexedChunk(string userId, DocumentChunk chunk) => new()
    {
        ChunkId = chunk.Id, UserId = userId, DocumentId = chunk.DocumentId, DocumentName = chunk.DocumentName,
        Content = chunk.Content, MetadataJson = JsonSerializer.Serialize(chunk.Metadata), IndexedAt = DateTime.UtcNow
    };

    private static RetrievedContext ToContext(Bm25IndexedChunk chunk) => new()
    {
        ChunkId = chunk.ChunkId, DocumentId = chunk.DocumentId, DocumentName = chunk.DocumentName, Content = chunk.Content,
        Distance = null, Similarity = null, DisplayScore = null,
        Metadata = JsonSerializer.Deserialize<Dictionary<string, string>>(chunk.MetadataJson) ?? []
    };

    private sealed record TokenizedChunk(Bm25IndexedChunk Chunk, List<string> Terms);

    private static IEnumerable<string> Tokenize(string text) => TokenPattern().Matches(text.ToLowerInvariant()).Select(match => match.Value);

    [GeneratedRegex("[\\p{L}\\p{Nd}]+", RegexOptions.CultureInvariant)]
    private static partial Regex TokenPattern();
}
