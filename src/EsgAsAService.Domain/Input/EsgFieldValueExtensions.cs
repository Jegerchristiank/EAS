using System;
using System.Globalization;

namespace EsgAsAService.Domain.Input;

public static class EsgFieldValueExtensions
{
    public static bool TryGetDecimal(this EsgFieldValue value, out decimal result)
    {
        switch (value)
        {
            case EsgFieldValue.Number number:
                result = number.Value;
                return true;
            case EsgFieldValue.Integer integer:
                result = integer.Value;
                return true;
            case EsgFieldValue.Text text when decimal.TryParse(text.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed):
                result = parsed;
                return true;
            default:
                result = default;
                return false;
        }
    }

    public static bool TryGetBoolean(this EsgFieldValue value, out bool result)
    {
        switch (value)
        {
            case EsgFieldValue.Boolean boolean:
                result = boolean.Value;
                return true;
            case EsgFieldValue.Text text:
                var normalized = text.Value?.Trim().ToLowerInvariant();
                if (normalized is "ja" or "true" or "1")
                {
                    result = true;
                    return true;
                }
                if (normalized is "nej" or "false" or "0")
                {
                    result = false;
                    return true;
                }
                break;
        }

        result = default;
        return false;
    }

    public static bool TryGetString(this EsgFieldValue value, out string? result)
    {
        switch (value)
        {
            case EsgFieldValue.Text text:
                result = text.Value;
                return true;
            default:
                result = null;
                return false;
        }
    }
}
