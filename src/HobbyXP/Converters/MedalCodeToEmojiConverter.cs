using System.Globalization;
using System.Windows.Data;
using HobbyXP.Models.Enums;

namespace HobbyXP.Converters;

public sealed class MedalCodeToEmojiConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is MedalCode code ? code switch
        {
            MedalCode.GoldRace => "🏅",
            MedalCode.PlatinumGame => "💎",
            MedalCode.ProgressiveOverload => "🏋️",
            _ => "⭐"
        } : "⭐";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
