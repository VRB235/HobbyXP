namespace HobbyXP.Services.Abstractions;

public interface IImagePreviewService
{
    /// <summary>Muestra la imagen a tamaño grande en un diálogo de la app.</summary>
    void Show(string filePath, string? title = null);

    /// <summary>Abre el archivo con la aplicación predeterminada de Windows.</summary>
    void OpenExternally(string filePath);
}
