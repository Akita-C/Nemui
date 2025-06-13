using Microsoft.EntityFrameworkCore;
using Nemui.Application.Repositories;
using Nemui.Infrastructure.Data.Context;
using Nemui.Shared.DTOs.Quiz;
using Nemui.Shared.Entities;

namespace Nemui.Infrastructure.Data.Repositories;

public class QuestionRepository(AppDbContext context) : Repository<Question>(context), IQuestionRepository
{
    private static readonly string[] DefaultInclues = ["Quiz", "Quiz.Creator"];

    public async ValueTask<Question?> GetByIdWithQuizAsync(Guid id, CancellationToken cancellationToken = default) =>
        await BuildQuestionQuery()
            .FirstOrDefaultAsync(q => q.Id == id, cancellationToken);

    public async ValueTask<(IEnumerable<Question> Questions, string? NextCursor)> GetQuestionsWithPaginationAsync(
        QuestionListRequest request, CancellationToken cancellationToken = default)
    {
        var query = BuildQuestionQuery();

        query = ApplyQuestionFilters(query, request);
        query = ApplyCursorPagination(query, request);
        query = ApplyQuesitonSorting(query, request);

        var questions = await query
            .Take(request.PageSize + 1)
            .ToListAsync(cancellationToken);

        var (resultQuestion, nextCursor) = ProcessPaginationResult(questions, request.PageSize);
        return (resultQuestion, nextCursor);
    }

    public async ValueTask<IEnumerable<Question>> GetQuestionsByQuizAsync(Guid quizId, CancellationToken cancellationToken = default) =>
        await BuildQuestionQuery()
            .Where(q => q.QuizId == quizId && !q.IsDeleted)
            .OrderBy(q => q.CreatedAt)
            .ToListAsync(cancellationToken);

    public ValueTask<bool> ExistsByContentAndQuizAsync(string content, Guid quizId, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(q => q.Content == content && q.QuizId == quizId && !q.IsDeleted);
        
        if (excludeId.HasValue)
            query = query.Where(q => q.Id != excludeId.Value);

        return new ValueTask<bool>(query.AnyAsync(cancellationToken));
    }

    public ValueTask<int> GetQuestionCountByQuizAsync(Guid quizId, CancellationToken cancellationToken = default) => 
        new(_dbSet.CountAsync(q => q.QuizId == quizId && !q.IsDeleted, cancellationToken));

    public async ValueTask<IEnumerable<Question>> GetQuestionsByQuizIdsAsync(IEnumerable<Guid> quizIds, CancellationToken cancellationToken = default)
    {
        var quizIdList = quizIds as List<Guid> ?? quizIds.ToList();
        
        return await BuildQuestionQuery()
            .Where(q => quizIdList.Contains(q.QuizId) && !q.IsDeleted)
            .OrderBy(q => q.QuizId)
            .ThenBy(q => q.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async ValueTask<Dictionary<Guid, int>> GetQuestionCountsByQuizIdsAsync(IEnumerable<Guid> quizIds, CancellationToken cancellationToken = default)
    {
        var quizIdList = quizIds as List<Guid> ?? quizIds.ToList();

        return await _dbSet
            .Where(q => quizIdList.Contains(q.QuizId) && !q.IsDeleted)
            .GroupBy(q => q.QuizId)
            .Select(g => new { QuizId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.QuizId, x => x.Count, cancellationToken);
    }

    public async ValueTask<IEnumerable<Question>> BulkCreateQuestionsAsync(IEnumerable<Question> questions, CancellationToken cancellationToken = default)
    {
        var questionList = questions as List<Question> ?? questions.ToList();
        return await BulkInsertAsync(questionList, cancellationToken);
    }

    public async ValueTask<bool> ExistsDuplicateContentInQuizAsync(IEnumerable<string> contents, Guid quizId,
        CancellationToken cancellationToken = default)
    {
        var contentList = contents as List<string> ?? contents.ToList();
        var existingCount = await _dbSet
            .Where(q => contentList.Contains(q.Content) && q.QuizId == quizId && !q.IsDeleted)
            .CountAsync(cancellationToken);
        
        return existingCount > 0;
    }
    
    private IQueryable<Question> BuildQuestionQuery() =>
        _dbSet
            .Include(q => q.Quiz)
            .ThenInclude(q => q.Creator)
            .Where(q => !q.IsDeleted);

    private static IQueryable<Question> ApplyQuestionFilters(IQueryable<Question> query, QuestionListRequest request)
    {
        if (request.QuizId.HasValue)
            query = query.Where(q => q.QuizId == request.QuizId.Value);

        if (!string.IsNullOrEmpty(request.Search))
        {
            var searchTerm = request.Search.ToLower();
            query = query.Where(q => 
                q.Content.Contains(searchTerm, StringComparison.CurrentCultureIgnoreCase) ||
                (q.Explanation != null && q.Explanation.Contains(searchTerm, StringComparison.CurrentCultureIgnoreCase)));
        }
        
        if (request.QuestionType.HasValue)
            query = query.Where(q => q.QuestionType == request.QuestionType.Value);
        
        return query;
    }

    private static IQueryable<Question> ApplyCursorPagination(IQueryable<Question> query, QuestionListRequest request)
    {
        if (string.IsNullOrEmpty(request.Cursor) || !DateTime.TryParse(request.Cursor, out var cursorDate))
            return query;
        
        return request.IsDescending
            ? query.Where(q => q.CreatedAt < cursorDate)
            : query.Where(q => q.CreatedAt > cursorDate);
    }

    private static IQueryable<Question> ApplyQuesitonSorting(IQueryable<Question> query, QuestionListRequest request) =>
        request.SortBy?.ToLower() switch
        {
            "content" => request.IsDescending
                ? query.OrderByDescending(q => q.Content)
                : query.OrderBy(q => q.Content),
            "points" => request.IsDescending
                ? query.OrderByDescending(q => q.Points)
                : query.OrderBy(q => q.Points),
            "createdat" => request.IsDescending
                ? query.OrderByDescending(q => q.CreatedAt)
                : query.OrderBy(q => q.CreatedAt),
            _ => request.IsDescending
                ? query.OrderByDescending(q => q.CreatedAt)
                : query.OrderBy(q => q.CreatedAt),
        };

    private static (IEnumerable<Question> questions, string? nextCursor) ProcessPaginationResult(
        List<Question> questions, int pageSize)
    {
        if (questions.Count <= pageSize)
            return (questions, null);
        
        var resultQuestions = questions.Take(pageSize);
        var nextCursor = questions[pageSize - 1].CreatedAt.ToString("0");
        
        return (resultQuestions, nextCursor);
    }
} 