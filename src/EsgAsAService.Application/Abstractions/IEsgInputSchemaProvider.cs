using EsgAsAService.Domain.Schema;

namespace EsgAsAService.Application.Abstractions;

/// <summary>
/// Provides access to the ESG input schema and supporting metadata derived from the datapoint catalogue.
/// </summary>
public interface IEsgInputSchemaProvider
{
    /// <summary>
    /// Loads the ESG input schema (cached once per application lifetime).
    /// </summary>
    ValueTask<EsgInputSchema> GetSchemaAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the formula mapping (datapoint → module/formula reference) for coverage checks.
    /// </summary>
    ValueTask<IReadOnlyDictionary<string, SchemaFormulaMapping>> GetFormulaMapAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Describes how a datapoint participates in module calculations.
/// </summary>
public sealed record SchemaFormulaMapping(
    string SectionCode,
    string DatapointKey,
    string? ModuleCalculation,
    string? FormulaReference,
    string CoverageStatus);

