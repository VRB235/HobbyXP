using HobbyXP.Data;
using HobbyXP.Helpers;
using HobbyXP.Models.Enums;
using HobbyXP.Models.Feedback;
using HobbyXP.Services.Abstractions;
using HobbyXP.Services.Results;
using Microsoft.EntityFrameworkCore;

namespace HobbyXP.Services;

public sealed class SuggestionService : ISuggestionService
{
    private readonly IDbContextFactory<HobbyXpDbContext> _dbContextFactory;

    public SuggestionService(IDbContextFactory<HobbyXpDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<IReadOnlyList<Suggestion>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Suggestions
            .AsNoTracking()
            .OrderByDescending(s => s.ReportedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<OperationResult<Suggestion>> CreateAsync(
        string title,
        string description,
        SuggestionKind kind,
        IReadOnlyList<string>? photoPaths = null,
        DateTime? reportedAt = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("El título es obligatorio.", nameof(title));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("La descripción es obligatoria.", nameof(description));

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var suggestion = new Suggestion
        {
            Title = title.Trim(),
            Description = description.Trim(),
            Kind = kind,
            Status = SuggestionStatus.Open,
            ReportedAt = reportedAt ?? DateTime.UtcNow
        };

        db.Suggestions.Add(suggestion);
        await db.SaveChangesAsync(cancellationToken);

        if (photoPaths is { Count: > 0 })
        {
            var savedPhotos = SuggestionPhotoStorage.SavePhotos(suggestion.Id, photoPaths);
            suggestion.PhotoPath = SuggestionPhotoStorage.Serialize(savedPhotos);
            await db.SaveChangesAsync(cancellationToken);
        }

        return OperationResult<Suggestion>.Empty(suggestion);
    }

    public async Task<OperationResult<Suggestion>> SetResolvedAsync(
        int suggestionId,
        bool resolved,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var suggestion = await db.Suggestions.FindAsync([suggestionId], cancellationToken)
            ?? throw new InvalidOperationException("La sugerencia no existe.");

        suggestion.Status = resolved ? SuggestionStatus.Resolved : SuggestionStatus.Open;
        suggestion.ResolvedAt = resolved ? DateTime.UtcNow : null;
        suggestion.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return OperationResult<Suggestion>.Empty(suggestion);
    }

    public async Task<bool> DeleteAsync(int suggestionId, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var suggestion = await db.Suggestions.FindAsync([suggestionId], cancellationToken);
        if (suggestion is null)
            return false;

        SuggestionPhotoStorage.DeleteStoredPhotos(suggestionId, suggestion.PhotoPath);
        db.Suggestions.Remove(suggestion);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
