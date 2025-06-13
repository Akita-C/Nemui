using Nemui.Application.Common.Interfaces;
using Nemui.Shared.DTOs.Quiz;
using Nemui.Shared.Entities;

namespace Nemui.Application.Repositories;

public interface IQuestionRepository : IRepository<Question>
{
    ValueTask<Question?> GetByIdWithQuizAsync(Guid id, CancellationToken cancellationToken = default);
    ValueTask<(IEnumerable<Question> Questions, string? NextCursor)> GetQuestionsWithPaginationAsync(
        QuestionListRequest request, CancellationToken cancellationToken = default);
    ValueTask<IEnumerable<Question>> GetQuestionsByQuizAsync(Guid quizId, CancellationToken cancellationToken = default);
    ValueTask<bool> ExistsByContentAndQuizAsync(string content, Guid quizId, Guid? excludeId = null, CancellationToken cancellationToken = default);
    ValueTask<int> GetQuestionCountByQuizAsync(Guid quizId, CancellationToken cancellationToken = default);
    ValueTask<IEnumerable<Question>> GetQuestionsByQuizIdsAsync(IEnumerable<Guid> quizIds, CancellationToken cancellationToken = default);
    ValueTask<Dictionary<Guid, int>> GetQuestionCountsByQuizIdsAsync(IEnumerable<Guid> quizIds, CancellationToken cancellationToken = default);
    ValueTask<IEnumerable<Question>> BulkCreateQuestionsAsync(IEnumerable<Question> questions, CancellationToken cancellationToken = default);
    ValueTask<bool> ExistsDuplicateContentInQuizAsync(IEnumerable<string> contents, Guid quizId, CancellationToken cancellationToken = default);
} 