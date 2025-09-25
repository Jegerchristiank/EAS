using System.Globalization;

namespace EsgAsAService.Web.Services;

public static class FormatHelpers
{
    public static string Percentage(double value) => value.ToString("P0", CultureInfo.CurrentCulture);
    public static string Decimal(double value, int decimals = 2) => value.ToString($"N{decimals}", CultureInfo.CurrentCulture);
    public static string Currency(decimal value, string currency = "DKK") => string.Format(CultureInfo.CurrentCulture, "{0:C}", value) + $" {currency}";
    public static string TonnesCo2(double value) => string.Format(CultureInfo.CurrentCulture, "{0:N1} tCO₂e", value);
    public static string Date(DateTimeOffset? value) => value?.ToLocalTime().ToString("dd-MM-yyyy", CultureInfo.CurrentCulture) ?? "-";
    public static string IsoDate(DateOnly? value) => value?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty;
}
