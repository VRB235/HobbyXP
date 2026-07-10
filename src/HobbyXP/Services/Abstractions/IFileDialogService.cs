namespace HobbyXP.Services.Abstractions;

public interface IFileDialogService
{
    string? PickImageFile();

    IReadOnlyList<string> PickImageFiles();

    string? PickSaveFilePath(string suggestedFileName, string filter, string title);
}
