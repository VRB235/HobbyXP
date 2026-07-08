using HobbyXP.Models.Physical;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HobbyXP.Data.Configurations;

internal sealed class GymWorkoutEntryConfiguration : IEntityTypeConfiguration<GymWorkoutEntry>
{
    public void Configure(EntityTypeBuilder<GymWorkoutEntry> builder)
    {
        builder.ToTable("GymWorkoutEntries");

        builder.Property(e => e.Sets)
            .IsRequired();

        builder.Property(e => e.Repetitions)
            .IsRequired(false);

        builder.Property(e => e.WeightKg)
            .HasPrecision(8, 2)
            .IsRequired(false);

        builder.Property(e => e.Duration)
            .IsRequired(false);

        builder.Property(e => e.ExerciseType)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.HasOne(e => e.GymWorkout)
            .WithMany(w => w.Entries)
            .HasForeignKey(e => e.GymWorkoutId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Exercise)
            .WithMany(ex => ex.WorkoutEntries)
            .HasForeignKey(e => e.ExerciseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.ExerciseId, e.CreatedAt });
    }
}
