namespace __Product__.App.Hosting;

public static class Culture
{
    public static bool IsRtl { get; private set; }

    // WinUI has no render-time digit substitution, so Arabic-Indic digits are a WPF-only setting.
    public static bool ArabicIndicDigits => false;

    public static void Configure(AppSettings settings)
    {
        // Empty Language = follow the system locale (dates, numbers, calendar, direction).
        var culture = string.IsNullOrWhiteSpace(settings.Language)
            ? CultureInfo.CurrentCulture
            : CultureInfo.GetCultureInfo(settings.Language);

        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        IsRtl = culture.TextInfo.IsRightToLeft;
    }
}
