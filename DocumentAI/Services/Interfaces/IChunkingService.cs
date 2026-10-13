using DocumentAI.Models;

namespace DocumentAI.Services.Interfaces;

public interface IChunkingService
{
    IReadOnlyList<DocumentChunk> ChunkDocument(
        string documentId,
        string documentName,
        string text,
        int? chunkSize = null,
        int? chunkOverlap = null);
}

