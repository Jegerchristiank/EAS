namespace EsgAsAService.Domain.Schema;

/// <summary>
/// Immutable representation of the ESG wizard input schema derived from the datapoint catalogue CSV.
/// </summary>
public sealed record EsgInputSchema(
    IReadOnlyList<EsgSection> Sections,
    string Source,
    DateTimeOffset GeneratedAtUtc)
{
    public EsgSection GetSectionOrThrow(string sectionCode)
        => Sections.FirstOrDefault(s => string.Equals(s.SectionCode, sectionCode, StringComparison.OrdinalIgnoreCase))
           ?? throw new KeyNotFoundException($"Section '{sectionCode}' not found in schema");
}

/// <summary>
/// Represents a logical ESG section (e.g. B3) with its datapoints and metadata.
/// </summary>
public sealed record EsgSection(
    string Module,
    string SectionCode,
    string SectionName,
    IReadOnlyList<EsgField> Fields)
{
    public override string ToString() => $"{SectionCode} – {SectionName}";
}

/// <summary>
/// Describes an individual datapoint required for the wizard/calculation pipeline.
/// </summary>
public sealed record EsgField(
    string DatapointKey,
    string Label,
    EsgFieldDataType DataType,
    string? Unit,
    IReadOnlyList<string> AllowedValues,
    EsgFieldRequirement Requirement,
    FieldVisibilityCondition? Visibility,
    string? Notes)
{
    public bool IsEnumerated => AllowedValues.Count > 0;
}

/// <summary>
/// Supported CSV datatypes mapped to strongly typed wizard controls and validation.
/// </summary>
public enum EsgFieldDataType
{
    Boolean,
    String,
    StringArray,
    Number,
    Integer,
    Object,
    ObjectArray,
    StringList
}

/// <summary>
/// Requirement level derived from the CSV column "requirement".
/// </summary>
public enum EsgFieldRequirement
{
    Required,
    Optional,
    Conditional
}

/// <summary>
/// Represents an optional predicate controlling whether a field should be shown.
/// Expressions are stored verbatim and evaluated in the application layer.
/// </summary>
public sealed record FieldVisibilityCondition(string Expression)
{
    public override string ToString() => Expression;
}

