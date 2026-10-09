using HobbyXP.Models.Physical;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HobbyXP.Data.Configurations;

internal sealed class RunningSessionSeriesConfiguration : IEntityTypeConfiguration<RunningSessionSeries>
{
    public void Configure(EntityTypeBuilder<RunningSessionSeries> builder)
    {
        builder.ToTable("RunningSessionSeries");

        builder.Property(s => s.SortOrder)
            .IsRequired();

        builder.Property(s => s.DistanceKm)
            .HasPrecision(8, 3)
            .IsRequired();

        builder.Property(s => s.Duration)
            .IsRequired();

        builder.HasOne(s => s.RunningSession)
            .WithMany(r => r.Series)
            .HasForeignKey(s => s.RunningSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => new { s.RunningSessionId, s.SortOrder });
    }
}
