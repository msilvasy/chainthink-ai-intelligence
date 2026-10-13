using System.ComponentModel.DataAnnotations;

namespace DocumentAI.DTOs.Requests;

public sealed class UploadDocumentRequest
{
    [Required]
    public required IFormFile File { get; init; }
    
    public Dictionary<string, string>? Metadata { get; init; }
}

