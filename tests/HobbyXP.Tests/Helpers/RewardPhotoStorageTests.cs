using System.IO;
using HobbyXP.Helpers;
using HobbyXP.Models.Enums;
using HobbyXP.Services;

namespace HobbyXP.Tests.Helpers;

public sealed class RewardPhotoStorageTests : IDisposable
{
    private readonly string _dataDir;
    private readonly string _previousOverride;

    public RewardPhotoStorageTests()
    {
        _previousOverride = Environment.GetEnvironmentVariable("HOBBYXP_DATA_DIR") ?? string.Empty;
        _dataDir = Path.Combine(Path.GetTempPath(), "HobbyXP-RewardPhotoTests-" + Guid.NewGuid().ToString("N"));
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

        var staged = RewardPhotoStorage.ImportToStaging(original);
        Assert.NotNull(staged);
        Assert.True(File.Exists(staged));

        File.Delete(original);

        Assert.True(File.Exists(staged));
        Assert.True(RewardPhotoStorage.IsManagedPath(staged));
    }

    [Fact]
    public void SaveFromSource_SurvivesDeletingOriginal()
    {
        var original = CreateTempImage("buy-me.jpg");

        var stored = RewardPhotoStorage.SaveFromSource(42, original);
        Assert.NotNull(stored);
        Assert.False(Path.IsPathRooted(stored));

        File.Delete(original);

        var resolved = RewardPhotoStorage.ResolveAbsolutePath(stored);
        Assert.NotNull(resolved);
        Assert.True(File.Exists(resolved));
        Assert.True(RewardPhotoStorage.IsManagedForReward(42, stored));
    }

    [Fact]
    public async Task CreateAsync_PersistsManagedCopyIndependentOfSource()
    {
        var original = CreateTempImage("reward.png");
        var factory = new TestDbContextFactory();
        try
        {
            var xp = new XpService(factory, new FakeLevelUpMessenger());
            var profile = new PlayerProfileService(factory, new FakeProfileRefreshMessenger());
            var sut = new RewardService(factory, xp, profile);

            var reward = await sut.CreateAsync(
                "Auriculares",
                100,
                MilestoneSourceType.Gym,
                imageSourcePath: original);

            File.Delete(original);

            Assert.NotNull(reward.ImagePath);
            var absolute = RewardPhotoStorage.ResolveAbsolutePath(reward.ImagePath);
            Assert.NotNull(absolute);
            Assert.True(File.Exists(absolute));
        }
        finally
        {
            factory.Dispose();
        }
    }

    private string CreateTempImage(string fileName)
    {
        var path = Path.Combine(_dataDir, fileName);
        // PNG mínimo 1x1
        File.WriteAllBytes(path,
        [
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D,
            0x49, 0x48, 0x44, 0x52, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,
            0x08, 0x02, 0x00, 0x00, 0x00, 0x90, 0x77, 0x53, 0xDE, 0x00, 0x00, 0x00,
            0x0C, 0x49, 0x44, 0x41, 0x54, 0x08, 0xD7, 0x63, 0xF8, 0xCF, 0xC0, 0x00,
            0x00, 0x00, 0x03, 0x00, 0x01, 0x00, 0x05, 0xFE, 0x02, 0xFE, 0x00, 0x00,
            0x00, 0x00, 0x49, 0x45, 0x4E, 0x44, 0xAE, 0x42, 0x60, 0x82
        ]);
        return path;
    }
}
