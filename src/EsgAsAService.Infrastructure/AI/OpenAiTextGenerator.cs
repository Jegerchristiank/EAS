using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EsgAsAService.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EsgAsAService.Infrastructure.AI;

public class OpenAiOptions
{
    public string? ApiKey { get; set; }
    public string BaseUrl { get; set; } = "https://api.openai.com/v1"; // or Azure-compatible endpoint
    public string Model { get; set; } = "gpt-5";
}

public class OpenAiTextGenerator(HttpClient http, IOptions<OpenAiOptions> options, ILogger<OpenAiTextGenerator> logger)
    : IAITextGenerator
{
    private readonly HttpClient _http = http;
    private readonly OpenAiOptions _opt = options.Value;
    private readonly ILogger<OpenAiTextGenerator> _logger = logger;

    private sealed record ChatRequest(string model, ChatMessage[] messages, double temperature = 0.2);
    private sealed record ChatMessage(string role, string content);
    private sealed record ChatResponse(Choice[] choices);
    private sealed record Choice(Message message);
    private sealed record Message(string role, string content);

    public async Task<string> GenerateAsync(NarrativeRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_opt.ApiKey))
        {
            return "[AI disabled: set OPENAI_API_KEY to enable narrative generation]";
        }

        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _opt.ApiKey);

        var system = "You are an ESG reporting assistant. Generate concise, compliant narrative text for EU VSME disclosures. Keep tone professional and SME-friendly.";
        var user = $$"""
        Company: {request.CompanyName}
        Year: {request.Year}
        Section: {request.Section}
        Input summary:
        {request.InputSummary}
        Please draft a 2-3 paragraph narrative including 1-3 improvement initiatives.
        """;

        var payload = new ChatRequest(_opt.Model, new[]
        {
            new ChatMessage("system", system),
            new ChatMessage("user", user)
        });

        var url = _opt.BaseUrl.TrimEnd('/') + "/chat/completions";
        try
        {
            var resp = await _http.PostAsJsonAsync(url, payload, cancellationToken: ct);
            resp.EnsureSuccessStatusCode();
            var content = await resp.Content.ReadFromJsonAsync<ChatResponse>(cancellationToken: ct);
            return content?.choices?.FirstOrDefault()?.message?.content?.Trim() ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OpenAI call failed, returning placeholder text");
            return "[Unable to generate narrative at this time.]";
        }
    }
}
