using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;

namespace HobbyXP.Helpers;

public static class LiveChartsThemeSetup
{
    public static void ApplyDarkTheme() =>
        LiveCharts.Configure(config => config.AddDarkTheme());
}
