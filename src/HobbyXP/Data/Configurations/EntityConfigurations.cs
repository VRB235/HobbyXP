using HobbyXP.Models.Achievements;
using HobbyXP.Models.Core;
using HobbyXP.Models.Entertainment;
using HobbyXP.Models.Enums;
using HobbyXP.Models.PersonalGrowth;
using HobbyXP.Models.Physical;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HobbyXP.Data.Configurations;

internal sealed class PlayerProfileConfiguration : IEntityTypeConfiguration<PlayerProfile>
{
    public void Configure(EntityTypeBuilder<PlayerProfile> builder)
    {
        builder.ToTable("PlayerProfiles");

        builder.Property(p => p.CurrentLevel)
            .HasDefaultValue(1);

        builder.Property(p => p.BaseXpPerLevel)
            .HasDefaultValue(1000);

        builder.Property(p => p.SpendableXp)
            .HasDefaultValue(0);

        builder.Property(p => p.SpendableLedgerInitialized)
            .HasDefaultValue(false);

        builder.Property(p => p.SpendableProgressBaselineApplied)
            .HasDefaultValue(false);

        builder.Property(p => p.DisplayName)
            .HasMaxLength(100)
            .HasDefaultValue("Aventurero");

        builder.Property(p => p.AvatarPath)
            .HasMaxLength(500);
    }
}

internal sealed class XpTransactionConfiguration : IEntityTypeConfiguration<XpTransaction>
{
    public void Configure(EntityTypeBuilder<XpTransaction> builder)
    {
        builder.ToTable("XpTransactions");

        builder.Property(t => t.Description)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(t => t.ActionType)
            .HasConversion<string>()
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(t => t.SourceType)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(t => t.IsGlobal)
            .HasDefaultValue(false);

        builder.HasOne(t => t.PlayerProfile)
            .WithMany(p => p.Transactions)
            .HasForeignKey(t => t.PlayerProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => t.EarnedAt);
        builder.HasIndex(t => t.SourceType);
    }
}

internal sealed class HobbyProgressConfiguration : IEntityTypeConfiguration<HobbyProgress>
{
    public void Configure(EntityTypeBuilder<HobbyProgress> builder)
    {
        builder.ToTable("HobbyProgresses");

        builder.Property(h => h.SourceType)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(h => h.CurrentLevel)
            .HasDefaultValue(1);

        builder.HasOne(h => h.PlayerProfile)
            .WithMany(p => p.HobbyProgresses)
            .HasForeignKey(h => h.PlayerProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(h => new { h.PlayerProfileId, h.SourceType })
            .IsUnique();
    }
}

internal sealed class MilestoneConfiguration : IEntityTypeConfiguration<Milestone>
{
    public void Configure(EntityTypeBuilder<Milestone> builder)
    {
        builder.ToTable("Milestones");

        builder.Property(m => m.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(m => m.Description)
            .HasMaxLength(1000);

        builder.Property(m => m.SourceType)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.HasIndex(m => m.CompletedAt);
    }
}

internal sealed class ExerciseConfiguration : IEntityTypeConfiguration<Exercise>
{
    public void Configure(EntityTypeBuilder<Exercise> builder)
    {
        builder.ToTable("Exercises");

        builder.Property(e => e.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(e => e.ExerciseType)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(e => e.MuscleGroup)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.HasIndex(e => e.Name)
            .IsUnique();

        builder.HasIndex(e => e.MuscleGroup);
    }
}

internal sealed class GymWorkoutConfiguration : IEntityTypeConfiguration<GymWorkout>
{
    public void Configure(EntityTypeBuilder<GymWorkout> builder)
    {
        builder.ToTable("GymWorkouts");

        builder.Property(w => w.Notes)
            .HasMaxLength(1000);

        builder.HasIndex(w => w.WorkoutDate);
    }
}

internal sealed class PuzzleConfiguration : IEntityTypeConfiguration<Puzzle>
{
    public void Configure(EntityTypeBuilder<Puzzle> builder)
    {
        builder.ToTable("Puzzles");

        builder.Property(p => p.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.Category)
            .HasConversion<string>()
            .HasMaxLength(8)
            .IsRequired();

        builder.Property(p => p.PhotoPath)
            .HasMaxLength(2000)
            .IsRequired(false);
    }
}

internal sealed class MediaEntryConfiguration : IEntityTypeConfiguration<MediaEntry>
{
    public void Configure(EntityTypeBuilder<MediaEntry> builder)
    {
        builder.ToTable("MediaEntries");

        builder.Property(m => m.Title)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(m => m.MediaType)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.HasIndex(m => m.CompletedAt);
    }
}

internal sealed class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        builder.ToTable("Books");

        builder.Property(b => b.Title)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(b => b.Author)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(b => b.Status)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Books_PagesRead",
            "[PagesRead] >= 0 AND [PagesRead] <= [TotalPages]"));

        builder.HasIndex(b => b.Status);
    }
}

internal sealed class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        builder.ToTable("Courses");

        builder.Property(c => c.Name)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(c => c.Platform)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(c => c.Status)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Courses_SessionsCompleted",
            "[SessionsCompleted] >= 0 AND [SessionsCompleted] <= [TotalSessions]"));

        builder.HasIndex(c => c.Status);

        builder.HasMany(c => c.SessionLogs)
            .WithOne(l => l.Course)
            .HasForeignKey(l => l.CourseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class CourseSessionLogConfiguration : IEntityTypeConfiguration<CourseSessionLog>
{
    public void Configure(EntityTypeBuilder<CourseSessionLog> builder)
    {
        builder.ToTable("CourseSessionLogs");

        builder.Property(l => l.SessionsDone)
            .IsRequired();

        builder.HasIndex(l => new { l.CourseId, l.SessionDate });
    }
}

internal sealed class MedalDefinitionConfiguration : IEntityTypeConfiguration<MedalDefinition>
{
    public void Configure(EntityTypeBuilder<MedalDefinition> builder)
    {
        builder.ToTable("MedalDefinitions");

        builder.Property(m => m.Code)
            .HasConversion<string>()
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(m => m.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(m => m.Description)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(m => m.UnlockHint)
            .HasMaxLength(500)
            .IsRequired();

        builder.HasIndex(m => m.Code)
            .IsUnique();
    }
}

internal sealed class EarnedMedalConfiguration : IEntityTypeConfiguration<EarnedMedal>
{
    public void Configure(EntityTypeBuilder<EarnedMedal> builder)
    {
        builder.ToTable("EarnedMedals");

        builder.HasOne(e => e.MedalDefinition)
            .WithMany(m => m.EarnedInstances)
            .HasForeignKey(e => e.MedalDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.MedalDefinitionId, e.EarnedAt });
    }
}

internal sealed class AchievementRuleConfiguration : IEntityTypeConfiguration<AchievementRule>
{
    public void Configure(EntityTypeBuilder<AchievementRule> builder)
    {
        builder.ToTable("AchievementRules");

        builder.Property(r => r.ActionType)
            .HasConversion<string>()
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(r => r.DisplayName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(r => r.UnitLabel)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(r => r.PointsPerUnit)
            .HasPrecision(10, 2);

        builder.HasIndex(r => r.ActionType)
            .IsUnique();
    }
}

internal sealed class RewardConfiguration : IEntityTypeConfiguration<Reward>
{
    public void Configure(EntityTypeBuilder<Reward> builder)
    {
        builder.ToTable("Rewards");

        builder.Property(r => r.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(r => r.Description)
            .HasMaxLength(1000);

        builder.Property(r => r.Status)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.HasIndex(r => r.Status);
    }
}
