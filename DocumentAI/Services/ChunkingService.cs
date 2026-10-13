using System.Text;
using DocumentAI.Configuration;
using DocumentAI.Models;
using DocumentAI.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace DocumentAI.Services;

public sealed class ChunkingService : IChunkingService
{
    private readonly ChunkingSettings _settings;
    private readonly ILogger<ChunkingService> _logger;

    public ChunkingService(IOptions<ChunkingSettings> settings, ILogger<ChunkingService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public IReadOnlyList<DocumentChunk> ChunkDocument(
        string documentId,
        string documentName,
        string text,
        int? chunkSize = null,
        int? chunkOverlap = null)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            _logger.LogWarning("Empty text provided for document {DocumentId}", documentId);
            return [];
        }

        var effectiveChunkSize = chunkSize ?? _settings.ChunkSize;
        var effectiveChunkOverlap = chunkOverlap ?? _settings.ChunkOverlap;
        var chunks = new List<DocumentChunk>();
        var sentences = SplitIntoSentences(text);
        
        var currentChunk = new StringBuilder();
        int chunkIndex = 0;
        int startPosition = 0;
        int currentPosition = 0;

        foreach (var sentence in sentences)
        {
            var trimmedSentence = sentence.Trim();
            if (string.IsNullOrWhiteSpace(trimmedSentence))
            {
                currentPosition += sentence.Length;
                continue;
            }

            // Check if adding this sentence would exceed chunk size
            if (currentChunk.Length + trimmedSentence.Length > effectiveChunkSize && currentChunk.Length >= _settings.MinChunkSize)
            {
                // Create chunk
                chunks.Add(CreateChunk(documentId, documentName, currentChunk.ToString().Trim(), chunkIndex, startPosition, currentPosition));
                chunkIndex++;

                // Handle overlap - keep last portion of current chunk
                var overlapText = GetOverlapText(currentChunk.ToString(), effectiveChunkOverlap);
                currentChunk.Clear();
                currentChunk.Append(overlapText);
                startPosition = currentPosition - overlapText.Length;
            }

            currentChunk.Append(trimmedSentence);
            currentChunk.Append(' ');
            currentPosition += sentence.Length;
        }

        // Add final chunk if it meets minimum size
        if (currentChunk.Length >= _settings.MinChunkSize)
        {
            chunks.Add(CreateChunk(documentId, documentName, currentChunk.ToString().Trim(), chunkIndex, startPosition, currentPosition));
        }
        else if (currentChunk.Length > 0 && chunks.Count > 0)
        {
            // Append small remaining text to last chunk
            var lastChunk = chunks[^1];
            chunks[^1] = lastChunk with 
            { 
                Content = lastChunk.Content + " " + currentChunk.ToString().Trim(),
                EndPosition = currentPosition
            };
        }
        else if (currentChunk.Length > 0)
        {
            // Single small chunk - include it anyway
            chunks.Add(CreateChunk(documentId, documentName, currentChunk.ToString().Trim(), chunkIndex, startPosition, currentPosition));
        }

        _logger.LogInformation(
            "Created {ChunkCount} chunks for document {DocumentId} using ChunkSize {ChunkSize} and ChunkOverlap {ChunkOverlap}",
            chunks.Count, documentId, effectiveChunkSize, effectiveChunkOverlap);
        return chunks;
    }

    private static DocumentChunk CreateChunk(string documentId, string documentName, string content, int index, int startPos, int endPos)
    {
        return new DocumentChunk
        {
            Id = $"{documentId}_chunk_{index}",
            DocumentId = documentId,
            DocumentName = documentName,
            Content = content,
            ChunkIndex = index,
            StartPosition = startPos,
            EndPosition = endPos,
            Metadata = new Dictionary<string, string>
            {
                ["chunk_index"] = index.ToString(),
                ["document_id"] = documentId,
                ["document_name"] = documentName
            }
        };
    }

    private static IReadOnlyList<string> SplitIntoSentences(string text)
    {
        var sentences = new List<string>();
        var currentSentence = new StringBuilder();
        
        for (int i = 0; i < text.Length; i++)
        {
            currentSentence.Append(text[i]);
            
            // Check for sentence boundaries
            if (IsSentenceEnd(text, i))
            {
                sentences.Add(currentSentence.ToString());
                currentSentence.Clear();
            }
            // Also split on paragraph breaks
            else if (text[i] == '\n' && i + 1 < text.Length && text[i + 1] == '\n')
            {
                sentences.Add(currentSentence.ToString());
                currentSentence.Clear();
            }
        }

        if (currentSentence.Length > 0)
        {
            sentences.Add(currentSentence.ToString());
        }

        return sentences;
    }

    private static bool IsSentenceEnd(string text, int index)
    {
        if (index >= text.Length)
            return false;

        char c = text[index];
        
        // Check for sentence-ending punctuation
        if (c != '.' && c != '!' && c != '?')
            return false;

        // Look ahead for whitespace or end of text
        if (index + 1 >= text.Length)
            return true;

        char next = text[index + 1];
        
        // Must be followed by whitespace or newline
        if (!char.IsWhiteSpace(next))
            return false;

        // Avoid splitting on common abbreviations
        if (c == '.')
        {
            // Check for common abbreviations (simplified check)
            int wordStart = index - 1;
            while (wordStart >= 0 && char.IsLetter(text[wordStart]))
                wordStart--;
            wordStart++;

            if (wordStart < index)
            {
                var word = text.Substring(wordStart, index - wordStart).ToLowerInvariant();
                string[] commonAbbreviations = ["mr", "mrs", "ms", "dr", "prof", "sr", "jr", "vs", "etc", "inc", "ltd", "co", "no", "vol", "pg", "pp", "e.g", "i.e"];
                if (commonAbbreviations.Contains(word))
                    return false;
            }
        }

        return true;
    }

    private static string GetOverlapText(string text, int overlapSize)
    {
        if (string.IsNullOrEmpty(text) || overlapSize <= 0)
            return string.Empty;

        if (text.Length <= overlapSize)
            return text;

        // Try to break at word boundary
        var overlapStart = text.Length - overlapSize;
        var spaceIndex = text.IndexOf(' ', overlapStart);
        
        if (spaceIndex > 0 && spaceIndex < text.Length - 1)
        {
            return text[(spaceIndex + 1)..];
        }

        return text[overlapStart..];
    }
}

