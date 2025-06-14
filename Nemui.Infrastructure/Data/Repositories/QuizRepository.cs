using Microsoft.EntityFrameworkCore;
using Nemui.Application.Repositories;
using Nemui.Infrastructure.Data.Context;
using Nemui.Shared.DTOs.Quiz;
using Nemui.Shared.Entities;

namespace Nemui.Infrastructure.Data.Repositories;

public class QuizRepository(AppDbContext context) : Repository<Quiz>(context), IQuizRepository
{
    public async ValueTask<Quiz?> GetByIdWithCreatorAsync(Guid id, CancellationToken cancellationToken = default) =>
        await BuildQuizQuery()
            .FirstOrDefaultAsync(q => q.Id == id, cancellationToken);

    public async ValueTask<Quiz?> GetByIdWithQuestionsAsync(Guid id, CancellationToken cancellationToken = default) =>
        await BuildQuizQueryWithQuestions()
            .FirstOrDefaultAsync(q => q.Id == id, cancellationToken);

    public async ValueTask<(IEnumerable<Quiz> Quizzes, string? NextCursor)> GetQuizzesWithPaginationAsync(
        QuizListRequest request, CancellationToken cancellationToken = default)
    {
        var query = BuildQuizQueryWithQuestions();

        query = ApplyQuizFilters(query, request);
        query = ApplyCursorPagination(query, request);
        query = ApplyQuizSorting(query, request);

        var quizzes = await query
            .Take(request.PageSize + 1)
            .ToListAsync(cancellationToken);

        var (resultQuizzes, nextCursor) = ProcessPaginationResult(quizzes, request.PageSize);

        return (resultQuizzes, nextCursor);
    }

    public async ValueTask<IEnumerable<Quiz>> GetQuizzesByCreatorAsync(Guid creatorId,
        CancellationToken cancellationToken = default) =>
        await BuildQuizQueryWithQuestions()
            .Where(q => q.CreatorId == creatorId)
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync(cancellationToken);

    public ValueTask<bool> ExistsByTitleAndCreatorAsync(string title, Guid creatorId, Guid? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(q => q.Title == title && q.CreatorId == creatorId && !q.IsDeleted);

        if (excludeId.HasValue) query = query.Where(q => q.Id != excludeId.Value);

        return new ValueTask<bool>(query.AnyAsync(cancellationToken));
    }

    public async ValueTask<IEnumerable<Quiz>> GetPublicQuizzesAsync(int limit = 10,
        CancellationToken cancellationToken = default) =>
        await BuildQuizQuery()
            .Where(q => q.IsPublic && !q.IsDeleted)
            .OrderByDescending(q => q.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

    public async ValueTask<IEnumerable<Quiz>> GetQuizzesByIdsAsync(IEnumerable<Guid> quizIds,
        CancellationToken cancellationToken = default)
    {
        var quizIdList = quizIds as List<Guid> ?? quizIds.ToList();
        
        return await BuildQuizQueryWithQuestions()
            .Where(q => quizIdList.Contains(q.Id) && !q.IsDeleted)
            .ToListAsync(cancellationToken);
    }

    public async ValueTask<Dictionary<string, int>> GetQuizCountsByCategoryAsync(CancellationToken cancellationToken = default) =>
        await _dbSet
            .Where(q => !q.IsDeleted && q.IsPublic && !string.IsNullOrEmpty(q.Category))
            .GroupBy(q => q.Category!)
            .Select(g => new { Category = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Category, x => x.Count, cancellationToken);

    public async ValueTask<IEnumerable<Quiz>> GetQuizzesByCategoryAsync(string category, int limit = 20,
        CancellationToken cancellationToken = default) =>
        await BuildQuizQuery()
            .Where(q => q.Category == category && q.IsPublic && !q.IsDeleted)
            .OrderByDescending(q => q.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

    public async ValueTask<IEnumerable<Quiz>> SearchQuizzesAsync(string searchTerm, int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var lowerSearchTerm = searchTerm.ToLower();
        
        return await BuildQuizQuery()
            .Where(q => q.IsPublic && !q.IsDeleted && (
                EF.Functions.ILike(q.Title, $"%{lowerSearchTerm}%") ||
                (q.Description != null && EF.Functions.ILike(q.Description, $"%{lowerSearchTerm}%")) ||
                (q.Category != null && EF.Functions.ILike(q.Category, $"%{lowerSearchTerm}%"))
            ))
            .OrderByDescending(q => q.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async ValueTask<(IEnumerable<Quiz> Quizzes, string? NextCursor)> GetMyQuizzesWithPaginationAsync(
        Guid creatorId, QuizListRequest request, CancellationToken cancellationToken = default)
    {
        var query = BuildQuizQueryWithQuestions()
            .Where(q => q.CreatorId == creatorId); // Filter by creator first

        query = ApplyQuizFilters(query, request);
        query = ApplyCursorPagination(query, request);
        query = ApplyQuizSorting(query, request);

        var quizzes = await query
            .Take(request.PageSize + 1)
            .ToListAsync(cancellationToken);

        var (resultQuizzes, nextCursor) = ProcessPaginationResult(quizzes, request.PageSize);

        return (resultQuizzes, nextCursor);
    }

    private IQueryable<Quiz> BuildQuizQuery()
    {
        return _dbSet
            .Include(q => q.Creator)
            .Where(q => !q.IsDeleted);
    }

    private IQueryable<Quiz> BuildQuizQueryWithQuestions()
    {
        return _dbSet
            .Include(q => q.Creator)
            .Include(q => q.Questions.Where(question => !question.IsDeleted))
            .Where(q => !q.IsDeleted);
    }

    private static IQueryable<Quiz> ApplyQuizFilters(IQueryable<Quiz> query, QuizListRequest request)
    {
        if (!string.IsNullOrEmpty(request.Search))
        {
            var searchTerm = request.Search.ToLower();
            query = query.Where(q =>
                EF.Functions.ILike(q.Title, $"%{searchTerm}%") ||
                (q.Description != null &&
                 EF.Functions.ILike(q.Description, $"%{searchTerm}%")) ||
                (q.Category != null && EF.Functions.ILike(q.Category, $"%{searchTerm}%")));
        }

        if (!string.IsNullOrEmpty(request.Category))
            query = query.Where(q => q.Category == request.Category);

        if (request.IsPublic.HasValue)
            query = query.Where(q => q.IsPublic == request.IsPublic.Value);

        if (request.CreatorId.HasValue)
            query = query.Where(q => q.CreatorId == request.CreatorId.Value);

        return query;
    }

    private static IQueryable<Quiz> ApplyCursorPagination(IQueryable<Quiz> query, QuizListRequest request)
    {
        if (string.IsNullOrEmpty(request.Cursor) || !DateTime.TryParse(request.Cursor, out var cursorDate))
            return query;

        var utcCursorDate = cursorDate.ToUniversalTime();

        return request.IsDescending
            ? query.Where(q => q.CreatedAt < utcCursorDate)
            : query.Where(q => q.CreatedAt > utcCursorDate);
    }

    private static IQueryable<Quiz> ApplyQuizSorting(IQueryable<Quiz> query, QuizListRequest request)
    {
        return request.SortBy?.ToLower() switch
        {
            "title" => request.IsDescending
                ? query.OrderByDescending(q => q.Title)
                : query.OrderBy(q => q.Title),
            "createdat" => request.IsDescending
                ? query.OrderByDescending(q => q.CreatedAt)
                : query.OrderBy(q => q.CreatedAt),
            _ => request.IsDescending
                ? query.OrderByDescending(q => q.CreatedAt)
                : query.OrderBy(q => q.CreatedAt)
        };
    }

    private static (IEnumerable<Quiz> Quizzes, string? NextCursor) ProcessPaginationResult(
        List<Quiz> quizzes, int pageSize)
    {
        if (quizzes.Count <= pageSize)
            return (quizzes, null);

        var resultQuizzes = quizzes.Take(pageSize);
        var nextCursor = quizzes[pageSize - 1].CreatedAt.ToString("O");

        return (resultQuizzes, nextCursor);
    }
}