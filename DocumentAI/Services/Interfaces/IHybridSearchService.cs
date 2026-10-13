using DocumentAI.Models;

namespace DocumentAI.Services.Interfaces;

public interface IHybridSearchService
{
    Task<IReadOnlyList<RetrievedContext>> HybridSearchAsync(string userId, string query, int topK, string? documentId = null, IReadOnlyList<string>? documentIds = null, CancellationToken cancellationToken = default);
}
