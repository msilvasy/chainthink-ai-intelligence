using System.ComponentModel.DataAnnotations;

namespace DocumentAI.DTOs.Requests;

public sealed class QueryRequest
{
    [Required]
    [MinLength(3, ErrorMessage = "Query must be at least 3 characters")]
    [MaxLength(2000, ErrorMessage = "Query must not exceed 2000 characters")]
    public required string Query { get; init; }
    
    [Range(1, 20, ErrorMessage = "TopK must be between 1 and 20")]
    public int TopK { get; init; } = 5;

    public string? DocumentId { get; init; }

    public required string ModelId { get; set; }
    public List<string>? DocumentIds { get; set; }
}

