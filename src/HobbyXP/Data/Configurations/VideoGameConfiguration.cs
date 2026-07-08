using HobbyXP.Models.Entertainment;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HobbyXP.Data.Configurations;

internal sealed class VideoGameConfiguration : IEntityTypeConfiguration<VideoGame>
{
    public void Configure(EntityTypeBuilder<VideoGame> builder)
    {
        builder.ToTable("VideoGames");

        builder.Property(v => v.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(v => v.Platform)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(v => v.CompletionPercentage)
            .IsRequired();

        builder.Property(v => v.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_VideoGames_CompletionPercentage",
            "[CompletionPercentage] >= 0 AND [CompletionPercentage] <= 100"));

        builder.HasIndex(v => v.Status);
        builder.HasIndex(v => v.CompletionPercentage);
    }
}
