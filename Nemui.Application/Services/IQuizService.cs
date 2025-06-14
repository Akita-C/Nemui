using Nemui.Shared.DTOs.Quiz;

namespace Nemui.Application.Services;

public interface IQuizService
{
    ValueTask<QuizDto?> GetQuizByIdAsync(Guid id, CancellationToken cancellationToken = default);
    ValueTask<(IEnumerable<QuizSummaryDto> Quizzes, string? NextCursor)> GetQuizzesAsync(QuizListRequest request, CancellationToken cancellationToken = default);
    ValueTask<(IEnumerable<QuizSummaryDto> Quizzes, string? NextCursor)> GetMyQuizzesAsync(Guid userId, QuizListRequest request, CancellationToken cancellationToken = default);
    ValueTask<QuizDto> CreateQuizAsync(CreateQuizRequest request, Guid creatorId, CancellationToken cancellationToken = default);
    ValueTask<QuizDto> UpdateQuizAsync(Guid id, UpdateQuizRequest request, Guid userId, CancellationToken cancellationToken = default);
    ValueTask<bool> DeleteQuizAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
    ValueTask<bool> QuizExistsAsync(Guid id, CancellationToken cancellationToken = default);
    ValueTask<IEnumerable<QuizSummaryDto>> GetPublicQuizzesAsync(int limit = 10, CancellationToken cancellationToken = default);
    ValueTask<IEnumerable<QuizDto>> GetQuizzesByIdsAsync(IEnumerable<Guid> quizIds, CancellationToken cancellationToken = default);
    ValueTask<Dictionary<string, int>> GetQuizCountsByCategoryAsync(CancellationToken cancellationToken = default);
    ValueTask<IEnumerable<QuizSummaryDto>> GetQuizzesByCategoryAsync(string category, int limit = 20, CancellationToken cancellationToken = default);
    ValueTask<IEnumerable<QuizSummaryDto>> SearchQuizzesAsync(string searchTerm, int limit = 20, CancellationToken cancellationToken = default);
    ValueTask<bool> BulkDeleteQuizzesAsync(IEnumerable<Guid> quizIds, Guid userId, CancellationToken cancellationToken = default);
}