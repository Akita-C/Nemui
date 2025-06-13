using Nemui.Shared.DTOs.Quiz;

namespace Nemui.Application.Services;

public interface IQuestionService
{
    ValueTask<QuestionDto?> GetQuestionByIdAsync(Guid id, CancellationToken cancellationToken = default);
    ValueTask<(IEnumerable<QuestionDto> Questions, string? NextCursor)> GetQuestionsAsync(QuestionListRequest request, CancellationToken cancellationToken = default);
    ValueTask<IEnumerable<QuestionDto>> GetQuestionsByQuizAsync(Guid quizId, CancellationToken cancellationToken = default);
    ValueTask<QuestionDto> CreateQuestionAsync(CreateQuestionRequest request, Guid userId, CancellationToken cancellationToken = default);
    ValueTask<QuestionDto> UpdateQuestionAsync(Guid id, UpdateQuestionRequest request, Guid userId, CancellationToken cancellationToken = default);
    ValueTask<bool> DeleteQuestionAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
    ValueTask<bool> QuestionExistsAsync(Guid id, CancellationToken cancellationToken = default);
    ValueTask<BulkCreateQuestionsResponse> BulkCreateQuestionsAsync(BulkCreateQuestionsRequest request, Guid userId, CancellationToken cancellationToken = default);
    ValueTask<IEnumerable<QuestionDto>> GetQuestionsByQuizIdsAsync(IEnumerable<Guid> quizIds, CancellationToken cancellationToken = default);
    ValueTask<Dictionary<Guid, int>> GetQuestionCountsByQuizIdsAsync(IEnumerable<Guid> quizIds, CancellationToken cancellationToken = default);
    ValueTask<bool> BulkDeleteQuestionsAsync(IEnumerable<Guid> questionIds, Guid userId, CancellationToken cancellationToken = default);
    ValueTask<IEnumerable<QuestionDto>> BulkUpdateQuestionsAsync(IEnumerable<UpdateQuestionBulkItem> requests, Guid userId, CancellationToken cancellationToken = default);
} 