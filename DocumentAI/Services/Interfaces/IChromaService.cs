using DocumentAI.DTOs.Responses;
using DocumentAI.Models;

namespace DocumentAI.Services.Interfaces;

public interface IChromaService
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task StoreChunksAsync(string userId, IReadOnlyList<DocumentChunk> chunks, float[][] embeddings, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RetrievedContext>> QueryAsync(string userId, float[] queryEmbedding, int topK, string? documentId = null, IReadOnlyList<string>? documentIds = null, CancellationToken cancellationToken = default);
    Task DeleteDocumentAsync(string documentId, CancellationToken cancellationToken = default);
    Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmbeddedDocumentResponse>> GetEmbeddedDocumentsAsync(string userId,CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmbeddedChunkResponse>> GetDocumentChunksAsync(string userId, string documentId, CancellationToken cancellationToken = default);
    Task DeleteChunkAsync(string chunkId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Bm25RebuildChunk>> GetChunksForBm25RebuildAsync(CancellationToken cancellationToken = default);
}

