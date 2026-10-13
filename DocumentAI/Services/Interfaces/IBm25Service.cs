using DocumentAI.Models;

namespace DocumentAI.Services.Interfaces;

public interface IBm25Service
{
    Task IndexChunksAsync(string userId, IReadOnlyList<DocumentChunk> chunks, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RetrievedContext>> SearchAsync(string userId, string query, int topK, string? documentId = null, IReadOnlyList<string>? documentIds = null, CancellationToken cancellationToken = default);
    Task DeleteDocumentAsync(string documentId, CancellationToken cancellationToken = default);
    Task DeleteChunkAsync(string chunkId, CancellationToken cancellationToken = default);
    Task RebuildAsync(IReadOnlyList<Bm25RebuildChunk> chunks, CancellationToken cancellationToken = default);
}
