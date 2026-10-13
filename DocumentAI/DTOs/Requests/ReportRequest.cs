using System.ComponentModel.DataAnnotations;

namespace DocumentAI.DTOs.Requests;

public sealed class ReportRequest
{
    [Required]
    [MinLength(10, ErrorMessage = "Topic must be at least 10 characters")]
    [MaxLength(500, ErrorMessage = "Topic must not exceed 500 characters")]
    public required string Topic { get; init; }
    
    [Range(1, 20, ErrorMessage = "TopK must be between 1 and 20")]
    public int TopK { get; init; } = 10;

    public string? DocumentId { get; init; }
    public required string  ModelId { get; set; }

    public string? AdditionalInstructions { get; init; }
}

