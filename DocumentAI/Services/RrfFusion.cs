using DocumentAI.Models;

namespace DocumentAI.Services;

public static class RrfFusion
{
    public static IReadOnlyList<RetrievedContext> Fuse(
        IReadOnlyList<RetrievedContext> vectorResults,
        IReadOnlyList<RetrievedContext> bm25Results,
        int rrfK,
        int topK,
        Action<string, int?, int?, double>? onRanked = null)
    {
        if (rrfK < 1) throw new ArgumentOutOfRangeException(nameof(rrfK));
        if (topK < 1) return [];

        var candidates = new Dictionary<string, Candidate>(StringComparer.Ordinal);
        Add(vectorResults, isVector: true);
        Add(bm25Results, isVector: false);

        return candidates.Values
            .OrderByDescending(candidate => candidate.RrfScore)
            .ThenBy(candidate => candidate.Chunk.ChunkId, StringComparer.Ordinal)
            .Take(topK)
            .Select(candidate =>
            {
                onRanked?.Invoke(candidate.Chunk.ChunkId, candidate.VectorRank, candidate.Bm25Rank, candidate.RrfScore);
                return candidate.Chunk;
            })
            .ToList();

        void Add(IReadOnlyList<RetrievedContext> results, bool isVector)
        {
            for (var index = 0; index < results.Count; index++)
            {
                var chunk = results[index];
                var rank = index + 1;
                if (!candidates.TryGetValue(chunk.ChunkId, out var candidate))
                {
                    candidate = new Candidate { Chunk = chunk };
                    candidates.Add(chunk.ChunkId, candidate);
                }

                candidate.RrfScore += 1d / (rrfK + rank);
                if (isVector)
                {
                    candidate.VectorRank = rank;
                    candidate.Chunk = chunk;
                }
                else
                {
                    candidate.Bm25Rank = rank;
                }
            }
        }
    }

    private sealed class Candidate
    {
        public required RetrievedContext Chunk { get; set; }
        public int? VectorRank { get; set; }
        public int? Bm25Rank { get; set; }
        public double RrfScore { get; set; }
    }
}
