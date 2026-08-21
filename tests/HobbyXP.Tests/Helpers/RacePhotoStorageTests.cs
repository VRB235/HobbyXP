using System.IO;
using HobbyXP.Helpers;

namespace HobbyXP.Tests.Helpers;

public sealed class RacePhotoStorageTests : IDisposable
{
    private readonly string _dataDir;
    private readonly string _previousOverride;

    public RacePhotoStorageTests()
    {
        _previousOverride = Environment.GetEnvironmentVariable("HOBBYXP_DATA_DIR") ?? string.Empty;
        _dataDir = Path.Combine(Path.GetTempPath(), "HobbyXP-RacePhotoTests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dataDir);
        Environment.SetEnvironmentVariable("HOBBYXP_DATA_DIR", _dataDir);
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(
            "HOBBYXP_DATA_DIR",
            string.IsNullOrEmpty(_previousOverride) ? null : _previousOverride);

        if (Directory.Exists(_dataDir))
            Directory.Delete(_dataDir, recursive: true);
    }

    [Fact]
    public void ImportToStaging_SurvivesDeletingOriginal()
    {
        var original = CreateTempImage("original.png");

        var staged = RacePhotoStorage.ImportToStaging(original);
        Assert.NotNull(staged);
        Assert.True(File.Exists(staged));

        File.Delete(original);

        Assert.True(File.Exists(staged));
        Assert.True(RacePhotoStorage.IsManagedPath(staged));
    }

    [Fact]
    public void SaveFromSource_SurvivesDeletingOriginal()
    {
        var original = CreateTempImage("race.jpg");

        var stored = RacePhotoStorage.SaveFromSource(7, original);
        Assert.NotNull(stored);
        Assert.False(Path.IsPathRooted(stored));

        File.Delete(original);

        var resolved = RacePhotoStorage.ResolveAbsolutePath(stored);
        Assert.NotNull(resolved);
        Assert.True(File.Exists(resolved));
        Assert.True(RacePhotoStorage.IsManagedForRace(7, stored));
    }

    private string CreateTempImage(string fileName)
    {
        var path = Path.Combine(_dataDir, fileName);
        File.WriteAllBytes(path, [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
        return path;
    }
}
