namespace DocumentAI.Services.Interfaces;

public interface IDocumentParserService
{
    Task<string> ExtractTextAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default);
    bool IsSupportedFileType(string fileName);
    string[] GetSupportedExtensions();
}

