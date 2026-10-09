using HobbyXP.Models.Physical;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HobbyXP.Data.Configurations;

internal sealed class OfficialRaceConfiguration : IEntityTypeConfiguration<OfficialRace>
{
    public void Configure(EntityTypeBuilder<OfficialRace> builder)
    {
        builder.ToTable("OfficialRaces");

        builder.Property(r => r.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(r => r.DistanceKm)
            .HasPrecision(8, 3)
            .IsRequired();

        builder.Property(r => r.Location)
            .HasMaxLength(200);

        builder.Property(r => r.Description)
            .HasMaxLength(1000);

        builder.Property(r => r.ImagePath)
            .HasMaxLength(500);

        builder.HasIndex(r => r.IsCompleted);
    }
}
