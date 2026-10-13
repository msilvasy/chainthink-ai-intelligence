using System.Text;
using DocumentAI.Services.Interfaces;
using DocumentFormat.OpenXml.Packaging;
using UglyToad.PdfPig;

namespace DocumentAI.Services;

public sealed class DocumentParserService : IDocumentParserService
{
    private readonly ILogger<DocumentParserService> _logger;
    private static readonly string[] SupportedExtensions = [".pdf", ".docx", ".txt"];

    public DocumentParserService(ILogger<DocumentParserService> logger)
    {
        _logger = logger;
    }

    public async Task<string> ExtractTextAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        
        _logger.LogInformation("Extracting text from {FileName} with extension {Extension}", fileName, extension);

        return extension switch
        {
            ".pdf" => await ExtractFromPdfAsync(fileStream, cancellationToken),
            ".docx" => await ExtractFromDocxAsync(fileStream, cancellationToken),
            ".txt" => await ExtractFromTxtAsync(fileStream, cancellationToken),
            _ => throw new NotSupportedException($"File type '{extension}' is not supported. Supported types: {string.Join(", ", SupportedExtensions)}")
        };
    }

    public bool IsSupportedFileType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return SupportedExtensions.Contains(extension);
    }

    public string[] GetSupportedExtensions() => SupportedExtensions;

    private async Task<string> ExtractFromPdfAsync(Stream fileStream, CancellationToken cancellationToken)
    {
        using var memoryStream = new MemoryStream();
        await fileStream.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;

        var textBuilder = new StringBuilder();
        
        using var document = PdfDocument.Open(memoryStream);
        foreach (var page in document.GetPages())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var pageText = page.Text;
            if (!string.IsNullOrWhiteSpace(pageText))
            {
                textBuilder.AppendLine(pageText);
                textBuilder.AppendLine();
            }
        }

        var result = CleanText(textBuilder.ToString());
        _logger.LogInformation("Extracted {CharCount} characters from PDF", result.Length);
        return result;
    }

    private async Task<string> ExtractFromDocxAsync(Stream fileStream, CancellationToken cancellationToken)
    {
        using var memoryStream = new MemoryStream();
        await fileStream.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;

        var textBuilder = new StringBuilder();

        using var document = WordprocessingDocument.Open(memoryStream, false);
        var body = document.MainDocumentPart?.Document?.Body;
        
        if (body != null)
        {
            foreach (var paragraph in body.Descendants<DocumentFormat.OpenXml.Wordprocessing.Paragraph>())
            {
                cancellationToken.ThrowIfCancellationRequested();
                var paragraphText = paragraph.InnerText;
                if (!string.IsNullOrWhiteSpace(paragraphText))
                {
                    textBuilder.AppendLine(paragraphText);
                }
            }
        }

        var result = CleanText(textBuilder.ToString());
        _logger.LogInformation("Extracted {CharCount} characters from DOCX", result.Length);
        return result;
    }

    private async Task<string> ExtractFromTxtAsync(Stream fileStream, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(fileStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var content = await reader.ReadToEndAsync(cancellationToken);
        var result = CleanText(content);
        _logger.LogInformation("Extracted {CharCount} characters from TXT", result.Length);
        return result;
    }

    private static string CleanText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        // Normalize line endings
        text = text.Replace("\r\n", "\n").Replace("\r", "\n");
        
        // Remove excessive whitespace while preserving paragraph structure
        var lines = text.Split('\n')
            .Select(line => string.Join(" ", line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)))
            .ToList();
        
        // Remove excessive blank lines (more than 2 consecutive)
        var result = new StringBuilder();
        int consecutiveEmpty = 0;
        
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                consecutiveEmpty++;
                if (consecutiveEmpty <= 2)
                {
                    result.AppendLine();
                }
            }
            else
            {
                consecutiveEmpty = 0;
                result.AppendLine(line);
            }
        }

        return result.ToString().Trim();
    }
}

