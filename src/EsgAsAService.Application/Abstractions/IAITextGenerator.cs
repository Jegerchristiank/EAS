namespace EsgAsAService.Application.Abstractions;

public enum NarrativeSection
{
    Overview,
    Environmental,
    Social,
    Governance,
    ImprovementPlan
}

public record NarrativeRequest(
    NarrativeSection Section,
    string CompanyName,
    int Year,
    string InputSummary
);

/// <summary>
/// Produces narrative text (short ESG sections) from structured inputs.
/// Why: separate AI concerns and keep HTTP/model specifics out of controllers.
/// </summary>
public interface IAITextGenerator
{
    /// <summary>
    /// Generates a narrative for the given section/request.
    /// Returns placeholder text if AI is disabled.
    /// </summary>
    Task<string> GenerateAsync(NarrativeRequest request, CancellationToken ct = default);
}
