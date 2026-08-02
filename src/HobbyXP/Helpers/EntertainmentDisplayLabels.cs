using HobbyXP.Models.Enums;

namespace HobbyXP.Helpers;

public static class EntertainmentDisplayLabels
{
    public static string GetPuzzleCategory(PuzzleCategory category) =>
        category switch
        {
            PuzzleCategory.TwoD => "2D",
            PuzzleCategory.ThreeD => "3D",
            _ => category.ToString()
        };

    public static string GetMediaType(MediaType mediaType) =>
        mediaType switch
        {
            MediaType.Movie => "Película",
            MediaType.Series => "Serie",
            _ => mediaType.ToString()
        };

    public static string GetVideoGamePlatform(VideoGamePlatform platform) =>
        platform switch
        {
            VideoGamePlatform.Pc => "PC",
            VideoGamePlatform.Ps5 => "PS5",
            VideoGamePlatform.Ps4 => "PS4",
            VideoGamePlatform.XboxSeries => "Xbox Series",
            VideoGamePlatform.XboxOne => "Xbox One",
            VideoGamePlatform.NintendoSwitch => "Nintendo Switch",
            VideoGamePlatform.Mobile => "Móvil",
            VideoGamePlatform.Other => "Otra",
            _ => platform.ToString()
        };
}
