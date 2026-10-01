using Serilog;

namespace __Product__.App.Hosting;

public static class AppLogging
{
    // Daily rolling files in %LOCALAPPDATA%\__Product__\logs, written off the UI thread.
    public static void Configure()
    {
        var config = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .WriteTo.Async(a => a.File(
                Path.Combine(AppPaths.Logs, "app-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                formatProvider: CultureInfo.InvariantCulture));   // logs stay culture-invariant
#if DEBUG
        config.MinimumLevel.Debug();
#else
        config.MinimumLevel.Information();
#endif
        Log.Logger = config.CreateLogger();
    }
}
