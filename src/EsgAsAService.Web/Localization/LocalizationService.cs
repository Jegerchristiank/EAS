using System.Globalization;
using Microsoft.Extensions.Localization;

namespace EsgAsAService.Web.Localization;

public sealed class LocalizationService
{
    private readonly IStringLocalizer<SharedResources> _localizer;

    public event EventHandler? CultureChanged;

    public LocalizationService(IStringLocalizer<SharedResources> localizer)
    {
        _localizer = localizer;
    }

    public string this[string key] => _localizer[key];

    public CultureInfo CurrentCulture => CultureInfo.CurrentUICulture;

    public IReadOnlyList<CultureInfo> SupportedCultures { get; } = new[]
    {
        new CultureInfo("da-DK"),
        new CultureInfo("en-US"),
    };

    public void SetCulture(string culture)
    {
        var info = SupportedCultures.FirstOrDefault(c => string.Equals(c.Name, culture, StringComparison.OrdinalIgnoreCase));
        if (info is null)
        {
            return;
        }

        CultureInfo.DefaultThreadCurrentCulture = info;
        CultureInfo.DefaultThreadCurrentUICulture = info;
        CultureInfo.CurrentCulture = info;
        CultureInfo.CurrentUICulture = info;
        CultureChanged?.Invoke(this, EventArgs.Empty);
    }
}
