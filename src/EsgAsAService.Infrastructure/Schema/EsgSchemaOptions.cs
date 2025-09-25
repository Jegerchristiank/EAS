namespace EsgAsAService.Infrastructure.Schema;

public sealed class EsgSchemaOptions
{
    public string? CsvPath { get; set; }
    public string SchemaOutputPath { get; set; } = Path.Combine("schema", "input.schema.json");
    public string FormulaMapOutputPath { get; set; } = Path.Combine("schema", "formula_map.json");
}

