using System.ComponentModel.DataAnnotations;

namespace DocumentAI.DTOs.Requests;

public sealed class UpdateQuerySettingsRequest
{
    [Required]
    [RegularExpression("^(fast|balanced|accurate)$", ErrorMessage = "Mode must be fast, balanced, or accurate")]
    public required string Mode { get; init; }

    [Range(1, 20, ErrorMessage = "TopK must be between 1 and 20")]
    public int TopK { get; init; }

    [Range(128, 10000, ErrorMessage = "ChunkSize must be between 128 and 10000")]
    public int ChunkSize { get; init; }

    [Range(0, 5000, ErrorMessage = "ChunkOverlap must be between 0 and 5000")]
    public int ChunkOverlap { get; init; }
}
