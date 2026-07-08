using HobbyXP.Models.Physical;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HobbyXP.Data.Configurations;

internal sealed class RunningSessionConfiguration : IEntityTypeConfiguration<RunningSession>
{
    public void Configure(EntityTypeBuilder<RunningSession> builder)
    {
        builder.ToTable("RunningSessions");

        builder.Property(r => r.DistanceKm)
            .HasPrecision(8, 3)
            .IsRequired();

        builder.Property(r => r.Duration)
            .IsRequired();

        builder.Property(r => r.PaceMinPerKm)
            .HasPrecision(8, 3)
            .IsRequired();

        builder.Property(r => r.CarreraId)
            .IsRequired(false);

        builder.Property(r => r.Notes)
            .HasMaxLength(500);

        builder.HasOne(r => r.Carrera)
            .WithMany(c => c.TrainingSessions)
            .HasForeignKey(r => r.CarreraId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(r => r.RecordedAt);
        builder.HasIndex(r => r.CarreraId);
    }
}
