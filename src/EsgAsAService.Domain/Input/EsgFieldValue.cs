using System.Linq;
using System.Text.Json;

namespace EsgAsAService.Domain.Input;

/// <summary>
/// Discriminated union for strongly typed ESG input values derived from the datapoint schema.
/// </summary>
public abstract record EsgFieldValue
{
    private EsgFieldValue() { }

    public sealed record Boolean(bool Value) : EsgFieldValue;
    public sealed record Number(decimal Value) : EsgFieldValue;
    public sealed record Integer(long Value) : EsgFieldValue;
    public sealed record Text(string Value) : EsgFieldValue;
    public sealed record StringArray(IReadOnlyList<string> Values) : EsgFieldValue;
    public sealed record Object(JsonDocument Value) : EsgFieldValue;
    public sealed record ObjectArray(IReadOnlyList<JsonDocument> Values) : EsgFieldValue;
    public sealed record Null : EsgFieldValue;

    public static readonly Null Empty = new();

    public static EsgFieldValue FromBoolean(bool value) => new Boolean(value);
    public static EsgFieldValue FromNumber(decimal value) => new Number(value);
    public static EsgFieldValue FromInteger(long value) => new Integer(value);
    public static EsgFieldValue FromText(string value) => new Text(value);
    public static EsgFieldValue FromStringArray(IEnumerable<string> values) => new StringArray(values.ToArray());
    public static EsgFieldValue FromObject(JsonDocument value) => new Object(value);
    public static EsgFieldValue FromObjectArray(IEnumerable<JsonDocument> values) => new ObjectArray(values.ToArray());
}
