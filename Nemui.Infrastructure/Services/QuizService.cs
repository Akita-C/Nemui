using FluentValidation;
using Nemui.Application.Repositories;
using Nemui.Application.Services;
using Nemui.Shared.DTOs.Quiz;
using Nemui.Shared.Entities;

namespace Nemui.Infrastructure.Services;

public class QuizService : IQuizService
{
    private const string QuizFolder = "quiz-thumbnails";
    private static readonly SemaphoreSlim ConcurrencySemaphore = new(Environment.ProcessorCount * 2);
    
    private readonly IUnitOfWork _unitOfWork;
    private readonly IImageService _imageService;
    private readonly IValidator<CreateQuizRequest> _createValidator;
    private readonly IValidator<UpdateQuizRequest> _updateValidator;

    public QuizService(
        IUnitOfWork unitOfWork,
        IImageService imageService,
        IValidator<CreateQuizRequest> createValidator,
        IValidator<UpdateQuizRequest> updateValidator)
    {
        _unitOfWork = unitOfWork;
        _imageService = imageService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async ValueTask<QuizDto?> GetQuizByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var quiz = await _unitOfWork.Quizzes.GetByIdWithQuestionsAsync(id, cancellationToken);
        return quiz == null ? null : await MapToQuizDtoAsync(quiz, cancellationToken);
    }

    public async ValueTask<(IEnumerable<QuizSummaryDto> Quizzes, string? NextCursor)> GetQuizzesAsync(
        QuizListRequest request, CancellationToken cancellationToken = default)
    {
        var (quizzes, nextCursor) = await _unitOfWork.Quizzes.GetQuizzesWithPaginationAsync(request, cancellationToken);
        
        var quizDtos = await MapToQuizSummaryDtosAsync(quizzes, cancellationToken);
        return (quizDtos, nextCursor);
    }

    public async ValueTask<(IEnumerable<QuizSummaryDto> Quizzes, string? NextCursor)> GetMyQuizzesAsync(Guid userId, QuizListRequest request, CancellationToken cancellationToken = default)
    {
        var (quizzes, nextCursor) = await _unitOfWork.Quizzes.GetMyQuizzesWithPaginationAsync(userId, request, cancellationToken);
        var quizDtos = await MapToQuizSummaryDtosAsync(quizzes, cancellationToken);
        return (quizDtos, nextCursor);
    }

    public async ValueTask<IEnumerable<QuizSummaryDto>> GetPublicQuizzesAsync(int limit = 10, CancellationToken cancellationToken = default)
    {
        var quizzes = await _unitOfWork.Quizzes.GetPublicQuizzesAsync(limit, cancellationToken);
        return await MapToQuizSummaryDtosAsync(quizzes, cancellationToken);
    }
    
    public async ValueTask<IEnumerable<QuizDto>> GetQuizzesByIdsAsync(IEnumerable<Guid> quizIds, CancellationToken cancellationToken = default)
    {
        var quizzes = await _unitOfWork.Quizzes.GetQuizzesByIdsAsync(quizIds, cancellationToken);
        return await MapToQuizDtosAsync(quizzes, cancellationToken);
    }

    public async ValueTask<Dictionary<string, int>> GetQuizCountsByCategoryAsync(CancellationToken cancellationToken = default) => 
        await _unitOfWork.Quizzes.GetQuizCountsByCategoryAsync(cancellationToken);

    public async ValueTask<IEnumerable<QuizSummaryDto>> GetQuizzesByCategoryAsync(string category, int limit = 20, CancellationToken cancellationToken = default)
    {
        var quizzes = await _unitOfWork.Quizzes.GetQuizzesByCategoryAsync(category, limit, cancellationToken);
        return await MapToQuizSummaryDtosAsync(quizzes, cancellationToken);
    }

    public async ValueTask<IEnumerable<QuizSummaryDto>> SearchQuizzesAsync(string searchTerm, int limit = 20, CancellationToken cancellationToken = default)
    {
        var quizzes = await _unitOfWork.Quizzes.SearchQuizzesAsync(searchTerm, limit, cancellationToken);
        return await MapToQuizSummaryDtosAsync(quizzes, cancellationToken);
    }

    public async ValueTask<QuizDto> CreateQuizAsync(CreateQuizRequest request, Guid creatorId, CancellationToken cancellationToken = default)
    {
        await _createValidator.ValidateAndThrowAsync(request, cancellationToken);
        await ValidateTitleUniquenessAsync(request.Title, creatorId, cancellationToken);

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var quiz = await CreateQuizEntityAsync(request, creatorId, cancellationToken);

            await _unitOfWork.Quizzes.AddAsync(quiz, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            quiz = await _unitOfWork.Quizzes.GetByIdWithQuestionsAsync(quiz.Id, cancellationToken);
            return await MapToQuizDtoAsync(quiz!, cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
    
    public async ValueTask<QuizDto> UpdateQuizAsync(Guid id, UpdateQuizRequest request, Guid userId, CancellationToken cancellationToken = default)
    {
        await _updateValidator.ValidateAndThrowAsync(request, cancellationToken);

        var quiz = await GetQuizWithOwnershipValidationAsync(id, userId, cancellationToken);

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var oldThumbnailPublicId = quiz.ThumbnailPublicId;
            
            await UpdateQuizEntityAsync(quiz, request, cancellationToken);

            await _unitOfWork.Quizzes.UpdateAsync(quiz, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Clean up old thumbnail if necessary
            await CleanupOldThumbnailAsync(request.Thumbnail, oldThumbnailPublicId, cancellationToken);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            // Reload with questions
            quiz = await _unitOfWork.Quizzes.GetByIdWithQuestionsAsync(quiz.Id, cancellationToken);
            return await MapToQuizDtoAsync(quiz!, cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
    
    public async ValueTask<bool> DeleteQuizAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var quiz = await _unitOfWork.Quizzes.GetByIdAsync(id, cancellationToken);
        if (quiz == null) return false;

        if (quiz.CreatorId != userId)
            throw new UnauthorizedAccessException("You can only delete your own quizzes");

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await DeleteQuizWithCleanupAsync(quiz, cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            return true;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
    
    public async ValueTask<bool> BulkDeleteQuizzesAsync(IEnumerable<Guid> quizIds, Guid userId, CancellationToken cancellationToken = default)
    {
        var quizIdList = quizIds as List<Guid> ?? quizIds.ToList();
        if (quizIdList.Count == 0) return false;

        var quizzes = await _unitOfWork.Quizzes.GetQuizzesByIdsAsync(quizIdList, cancellationToken);
        var quizList = quizzes.Where(q => quizIdList.Contains(q.Id)).ToList();

        if (quizList.Any(q => q.CreatorId != userId))
            throw new UnauthorizedAccessException("You can only delete your own quizzes");

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            // Parallel cleanup
            var thumbnailPublicIds = quizList.Select(q => q.ThumbnailPublicId).ToList();
            
            // Bulk delete
            await _unitOfWork.Quizzes.BulkDeleteAsync(quizList, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Parallel image cleanup
            var imageCleanupTasks = thumbnailPublicIds
                .Where(id => !string.IsNullOrEmpty(id))
                .Select(async thumbnailPublicId =>
                {
                    await ConcurrencySemaphore.WaitAsync(cancellationToken);
                    try
                    {
                        await _imageService.DeleteImageAsync(thumbnailPublicId!, cancellationToken);
                    }
                    finally
                    {
                        ConcurrencySemaphore.Release();
                    }
                });

            await Task.WhenAll(imageCleanupTasks);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            
            return true;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async ValueTask<bool> QuizExistsAsync(Guid id, CancellationToken cancellationToken = default) => 
        await _unitOfWork.Quizzes.ExistsAsync(q => q.Id == id, cancellationToken);

    private async ValueTask ValidateTitleUniquenessAsync(string title, Guid creatorId, CancellationToken cancellationToken)
    {
        var titleExists = await _unitOfWork.Quizzes.ExistsByTitleAndCreatorAsync(title, creatorId, cancellationToken: cancellationToken);
        if (titleExists)
            throw new InvalidOperationException("A quiz with this title already exists");
    }

    private async ValueTask<Quiz> GetQuizWithOwnershipValidationAsync(Guid id, Guid userId, CancellationToken cancellationToken)
    {
        var quiz = await _unitOfWork.Quizzes.GetByIdWithCreatorAsync(id, cancellationToken);
        if (quiz == null)
            throw new InvalidOperationException("Quiz not found");

        if (quiz.CreatorId != userId)
            throw new UnauthorizedAccessException("You can only update your own quizzes");

        return quiz;
    }

    private async ValueTask<Quiz> CreateQuizEntityAsync(CreateQuizRequest request, Guid creatorId, CancellationToken cancellationToken)
    {
        var quiz = new Quiz
        {
            Title = request.Title,
            Description = request.Description,
            IsPublic = request.IsPublic,
            Category = request.Category,
            Tags = request.Tags,
            EstimatedDurationMinutes = request.EstimatedDurationMinutes,
            CreatorId = creatorId
        };

        if (request.Thumbnail != null)
        {
            var imageResponse = await _imageService.UploadImageAsync(request.Thumbnail, QuizFolder, cancellationToken);
            quiz.ThumbnailPublicId = imageResponse.PublicId;
            quiz.ThumbnailUrl = imageResponse.SecureUrl;
        }

        return quiz;
    }

    private async ValueTask UpdateQuizEntityAsync(Quiz quiz, UpdateQuizRequest request, CancellationToken cancellationToken)
    {
        quiz.Title = request.Title;
        quiz.Description = request.Description;
        quiz.IsPublic = request.IsPublic;
        quiz.Category = request.Category;
        quiz.Tags = request.Tags;
        quiz.EstimatedDurationMinutes = request.EstimatedDurationMinutes;

        // Handle thumbnail upload
        if (request.Thumbnail != null)
        {
            var imageResponse = await _imageService.UploadImageAsync(request.Thumbnail, QuizFolder, cancellationToken);
            quiz.ThumbnailPublicId = imageResponse.PublicId;
            quiz.ThumbnailUrl = imageResponse.SecureUrl;
        }
    }

    private async ValueTask CleanupOldThumbnailAsync(Microsoft.AspNetCore.Http.IFormFile? newThumbnail, string? oldThumbnailPublicId, CancellationToken cancellationToken)
    {
        if (newThumbnail != null && !string.IsNullOrEmpty(oldThumbnailPublicId))
            await _imageService.DeleteImageAsync(oldThumbnailPublicId, cancellationToken);
    }

    private async ValueTask DeleteQuizWithCleanupAsync(Quiz quiz, CancellationToken cancellationToken)
    {
        var thumbnailPublicId = quiz.ThumbnailPublicId;

        await _unitOfWork.Quizzes.DeleteAsync(quiz, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Delete thumbnail if exists
        if (!string.IsNullOrEmpty(thumbnailPublicId))
            await _imageService.DeleteImageAsync(thumbnailPublicId, cancellationToken);
    }

    private async ValueTask<IEnumerable<QuizDto>> MapToQuizDtosAsync(IEnumerable<Quiz> quizzes, CancellationToken cancellationToken)
    {
        var quizList = quizzes as List<Quiz> ?? quizzes.ToList();
        
        var dtoTasks = quizList.Select(q => MapToQuizDtoAsync(q, cancellationToken).AsTask());
        return await Task.WhenAll(dtoTasks);
    }

    private async ValueTask<IEnumerable<QuizSummaryDto>> MapToQuizSummaryDtosAsync(IEnumerable<Quiz> quizzes, CancellationToken cancellationToken)
    {
        var quizList = quizzes as List<Quiz> ?? quizzes.ToList();
        
        var dtoTasks = quizList.Select(q => MapToQuizSummaryDtoAsync(q, cancellationToken).AsTask());
        return await Task.WhenAll(dtoTasks);
    }
    
    private async ValueTask<QuizDto> MapToQuizDtoAsync(Quiz quiz, CancellationToken cancellationToken)
    {
        var dto = new QuizDto
        {
            Id = quiz.Id,
            Title = quiz.Title,
            Description = quiz.Description,
            ThumbnailUrl = quiz.ThumbnailUrl,
            IsPublic = quiz.IsPublic,
            Category = quiz.Category,
            Tags = quiz.Tags,
            EstimatedDurationMinutes = quiz.EstimatedDurationMinutes,
            CreatedAt = quiz.CreatedAt.DateTime,
            UpdatedAt = quiz.UpdatedAt?.DateTime,
            CreatorId = quiz.CreatorId,
            CreatorName = quiz.Creator?.Name ?? string.Empty,
            QuestionCount = quiz.Questions?.Count ?? 0
        };

        // Add thumbnail transformations if thumbnail exists
        if (!string.IsNullOrEmpty(quiz.ThumbnailPublicId))
        {
            var transformationTasks = new[]
            {
                ("small", "c_fill,w_200,h_150,q_auto,f_auto"),
                ("medium", "c_fill,w_400,h_300,q_auto,f_auto"),
                ("large", "c_fill,w_800,h_600,q_auto,f_auto")
            }.Select(async t => new
            {
                Key = t.Item1,
                Url = await _imageService.GetImageUrlWithTransformationAsync(quiz.ThumbnailPublicId, t.Item2, cancellationToken)
            });

            var transformations = await Task.WhenAll(transformationTasks);
            dto.ThumbnailTransformations = transformations.ToDictionary(t => t.Key, t => t.Url);
        }

        return dto;
    }

    private async ValueTask<QuizSummaryDto> MapToQuizSummaryDtoAsync(Quiz quiz, CancellationToken cancellationToken)
    {
        var dto = new QuizSummaryDto
        {
            Id = quiz.Id,
            Title = quiz.Title,
            Description = quiz.Description,
            ThumbnailUrl = quiz.ThumbnailUrl,
            IsPublic = quiz.IsPublic,
            Category = quiz.Category,
            EstimatedDurationMinutes = quiz.EstimatedDurationMinutes,
            CreatedAt = quiz.CreatedAt.DateTime,
            CreatorId = quiz.CreatorId,
            CreatorName = quiz.Creator?.Name ?? string.Empty,
            QuestionCount = quiz.Questions?.Count ?? 0
        };

        // Add thumbnail transformations if thumbnail exists
        if (!string.IsNullOrEmpty(quiz.ThumbnailPublicId))
        {
            var transformationTasks = new[]
            {
                ("small", "c_fill,w_200,h_150,q_auto,f_auto"),
                ("medium", "c_fill,w_400,h_300,q_auto,f_auto")
            }.Select(async t => new
            {
                Key = t.Item1,
                Url = await _imageService.GetImageUrlWithTransformationAsync(quiz.ThumbnailPublicId, t.Item2, cancellationToken)
            });

            var transformations = await Task.WhenAll(transformationTasks);
            dto.ThumbnailTransformations = transformations.ToDictionary(t => t.Key, t => t.Url);
        }

        return dto;
    }
}