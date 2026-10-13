using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DocumentAI.Configuration;
using DocumentAI.DTOs.Responses;
using DocumentAI.Models;
using DocumentAI.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace DocumentAI.Services;

public sealed class LlmService : ILlmService
{
    private readonly HttpClient _httpClient;
    private readonly OllamaSettings _settings;
    private readonly ILogger<LlmService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public LlmService(
        HttpClient httpClient,
        IOptions<OllamaSettings> settings,
        ILogger<LlmService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<(string Answer, bool FoundInContext)> AnswerQueryAsync(string modelId,
        string query,
        IReadOnlyList<RetrievedContext> contexts,
        CancellationToken cancellationToken = default)
    {
        if (contexts.Count == 0)
        {
            return ("Information not found in documents. No relevant context was retrieved.", false);
        }

        var contextText = BuildContextText(contexts);
        var prompt = BuildQueryPrompt(query, contextText);

        _logger.LogDebug("Sending query to LLM with {ContextCount} context chunks", contexts.Count);

        var response = await SendPromptAsync(modelId,prompt, cancellationToken);

        // Check if the response indicates information was not found
        var notFoundIndicators = new[]
        {
            "information not found",
            "not found in the documents",
            "cannot find",
            "no information available",
            "not mentioned in the context",
            "not present in the provided",
            "unable to find"
        };

        var foundInContext = !notFoundIndicators.Any(indicator => 
            response.Contains(indicator, StringComparison.OrdinalIgnoreCase));

        return (response, foundInContext);
    }

    public async Task<ReportContent> GenerateReportAsync(string modelId,
        string topic,
        IReadOnlyList<RetrievedContext> contexts,
        string? additionalInstructions = null,
        CancellationToken cancellationToken = default)
    {
        if (contexts.Count == 0)
        {
            return new ReportContent
            {
                ExecutiveSummary = "Unable to generate report. No relevant information was found in the documents.",
                KeyFindings = ["No relevant data found in the document corpus."],
                Risks = ["Insufficient data to assess risks."],
                Recommendations = ["Upload more relevant documents and try again."]
            };
        }

        var contextText = BuildContextText(contexts);
        var prompt = BuildReportPrompt(topic, contextText, additionalInstructions);

        _logger.LogDebug("Generating report for topic: {Topic} with {ContextCount} context chunks", topic, contexts.Count);

        var response = await SendPromptAsync(modelId,prompt, cancellationToken);

        return ParseReportResponse(response);
    }

    public async Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_settings.BaseUrl}/api/tags", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ollama health check failed");
            return false;
        }
    }

    private async Task<string> SendPromptAsync(string modelId, string prompt, CancellationToken cancellationToken)
    {
        var request = new OllamaGenerateRequest
        {
            Model =modelId,
            Prompt = prompt,
            Stream = false,
            Options = new OllamaOptions
            {
                Temperature = _settings.Temperature,
                NumPredict = _settings.MaxTokens
            }
        };
        _httpClient.DefaultRequestHeaders.Authorization =
new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _settings.ApiKey);
        var response = await _httpClient.PostAsJsonAsync(
            $"{_settings.BaseUrlCloud}/api/generate",
            request,
            JsonOptions,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OllamaGenerateResponse>(JsonOptions, cancellationToken);

        if (string.IsNullOrWhiteSpace(result?.Response))
        {
            throw new InvalidOperationException("Received empty response from Ollama");
        }

        return result.Response.Trim();
    }

    public async Task<string> GetModelsAsync(CancellationToken cancellationToken = default)
    {
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _settings.ApiKey);

        var response = await _httpClient.GetAsync(
            $"{_settings.BaseUrlCloud}/v1/models",
            cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    private static string BuildContextText(IReadOnlyList<RetrievedContext> contexts)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < contexts.Count; i++)
        {
            var ctx = contexts[i];
            sb.AppendLine($"[Source {i + 1}: {ctx.DocumentName}]");
            sb.AppendLine(ctx.Content);
            sb.AppendLine();
        }
        return sb.ToString();
    }

    private static string BuildQueryPrompt(string query, string contextText)
    {
        return $"""
            You are a helpful assistant that answers questions based ONLY on the provided context.

            STRICT RULES:
            1. Answer ONLY using information from the context below
            2. If the answer is not in the context, respond with: "Information not found in documents."
            3. Do not make up or infer information not explicitly stated
            4. Cite which source(s) you used when answering
            5. Be concise and direct

            CONTEXT:
            {contextText}

            QUESTION: {query}

            ANSWER:
            """;
    }

    private static string BuildReportPrompt(string topic, string contextText, string? additionalInstructions)
    {
        var additionalText = string.IsNullOrWhiteSpace(additionalInstructions) 
            ? "" 
            : $"\n\nADDITIONAL INSTRUCTIONS:\n{additionalInstructions}";

        return $"""
            You are a business analyst creating a structured report based ONLY on the provided context.

            STRICT RULES:
            1. Use ONLY information from the context below
            2. Do not make up or infer information not explicitly stated
            3. If information for a section is not available, state "No relevant information found in documents"
            4. Be specific and cite sources when possible

            FORMAT YOUR RESPONSE EXACTLY AS FOLLOWS:
            
            ## EXECUTIVE SUMMARY
            [2-3 sentence overview of key points about the topic]

            ## KEY FINDINGS
            - [Finding 1]
            - [Finding 2]
            - [Finding 3]
            (Add more as needed)

            ## RISKS
            - [Risk 1]
            - [Risk 2]
            (Add more as needed, or state "No risks identified in documents")

            ## RECOMMENDATIONS
            - [Recommendation 1]
            - [Recommendation 2]
            (Add more as needed)

            CONTEXT:
            {contextText}

            TOPIC: {topic}{additionalText}

            REPORT:
            """;
    }

    private ReportContent ParseReportResponse(string response)
    {
        var sections = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var currentSection = string.Empty;
        var currentContent = new StringBuilder();

        foreach (var line in response.Split('\n'))
        {
            var trimmedLine = line.Trim();
            
            if (trimmedLine.StartsWith("## "))
            {
                if (!string.IsNullOrEmpty(currentSection))
                {
                    sections[currentSection] = currentContent.ToString().Trim();
                }
                currentSection = trimmedLine[3..].Trim().ToUpperInvariant();
                currentContent.Clear();
            }
            else
            {
                currentContent.AppendLine(line);
            }
        }

        if (!string.IsNullOrEmpty(currentSection))
        {
            sections[currentSection] = currentContent.ToString().Trim();
        }

        return new ReportContent
        {
            ExecutiveSummary = GetSectionContent(sections, "EXECUTIVE SUMMARY"),
            KeyFindings = ParseBulletPoints(GetSectionContent(sections, "KEY FINDINGS")),
            Risks = ParseBulletPoints(GetSectionContent(sections, "RISKS")),
            Recommendations = ParseBulletPoints(GetSectionContent(sections, "RECOMMENDATIONS"))
        };
    }

    private static string GetSectionContent(Dictionary<string, string> sections, string sectionName)
    {
        return sections.TryGetValue(sectionName, out var content) 
            ? content 
            : "No information available.";
    }

    private static List<string> ParseBulletPoints(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return ["No information available."];

        var points = content
            .Split('\n')
            .Select(line => line.Trim())
            .Where(line => line.StartsWith("- ") || line.StartsWith("* ") || line.StartsWith("• "))
            .Select(line => line[2..].Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();

        return points.Count > 0 ? points : [content];
    }

    private sealed class OllamaGenerateRequest
    {
        [JsonPropertyName("model")]
        public required string Model { get; init; }

        [JsonPropertyName("prompt")]
        public required string Prompt { get; init; }

        [JsonPropertyName("stream")]
        public bool Stream { get; init; }

        [JsonPropertyName("options")]
        public OllamaOptions? Options { get; init; }
    }

    private sealed class OllamaOptions
    {
        [JsonPropertyName("temperature")]
        public float Temperature { get; init; }

        [JsonPropertyName("num_predict")]
        public int NumPredict { get; init; }
    }

    private sealed class OllamaGenerateResponse
    {
        [JsonPropertyName("response")]
        public string? Response { get; init; }

        [JsonPropertyName("done")]
        public bool Done { get; init; }
    }
}

