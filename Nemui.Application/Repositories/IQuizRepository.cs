using Nemui.Application.Common.Interfaces;
using Nemui.Shared.DTOs.Quiz;
using Nemui.Shared.Entities;

namespace Nemui.Application.Repositories;

public interface IQuizRepository : IRepository<Quiz>
{
    ValueTask<Quiz?> GetByIdWithCreatorAsync(Guid id, CancellationToken cancellationToken = default);
    ValueTask<Quiz?> GetByIdWithQuestionsAsync(Guid id, CancellationToken cancellationToken = default);
    ValueTask<(IEnumerable<Quiz> Quizzes, string? NextCursor)> GetQuizzesWithPaginationAsync(
        QuizListRequest request, CancellationToken cancellationToken = default);
    ValueTask<IEnumerable<Quiz>> GetQuizzesByCreatorAsync(Guid creatorId, CancellationToken cancellationToken = default);
    ValueTask<bool> ExistsByTitleAndCreatorAsync(string title, Guid creatorId, Guid? excludeId = null, CancellationToken cancellationToken = default);
    ValueTask<IEnumerable<Quiz>> GetPublicQuizzesAsync(int limit = 10, CancellationToken cancellationToken = default);
    ValueTask<IEnumerable<Quiz>> GetQuizzesByIdsAsync(IEnumerable<Guid> quizIds, CancellationToken cancellationToken = default );
    ValueTask<Dictionary<string, int>> GetQuizCountsByCategoryAsync(CancellationToken cancellationToken = default);
    ValueTask<IEnumerable<Quiz>> GetQuizzesByCategoryAsync(string category, int limit = 20, CancellationToken cancellationToken = default);
    ValueTask<IEnumerable<Quiz>> SearchQuizzesAsync(string searchTerm, int limit = 20, CancellationToken cancellationToken = default);
}