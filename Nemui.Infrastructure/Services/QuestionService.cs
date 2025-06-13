using FluentValidation;
using Microsoft.AspNetCore.Http;
using Nemui.Application.Repositories;
using Nemui.Application.Services;
using Nemui.Shared.DTOs.Quiz;
using Nemui.Shared.Entities;

namespace Nemui.Infrastructure.Services;

public class QuestionService(
    IUnitOfWork unitOfWork,
    IImageService imageService,
    IValidator<CreateQuestionRequest> createValidator,
    IValidator<UpdateQuestionRequest> updateValidator,
    IValidator<BulkCreateQuestionsRequest> bulkCreateValidator)
    : IQuestionService
{
    private const string QuestionFolder = "question-images";
    private static readonly SemaphoreSlim ConcurrencySemaphore = new(Environment.ProcessorCount * 2);

    public async ValueTask<QuestionDto?> GetQuestionByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var question = await unitOfWork.Questions.GetByIdWithQuizAsync(id, cancellationToken);
        return question == null ? null : await MapToQuestionDtoAsync(question, cancellationToken);
    }

    public async ValueTask<(IEnumerable<QuestionDto> Questions, string? NextCursor)> GetQuestionsAsync(
        QuestionListRequest request, CancellationToken cancellationToken = default)
    {
        var (questions, nextCursor) = await unitOfWork.Questions.GetQuestionsWithPaginationAsync(request, cancellationToken);
        var questionDtos = await MapToQuestionDtosAsync(questions, cancellationToken);
        return (questionDtos, nextCursor);
    }

    public async ValueTask<IEnumerable<QuestionDto>> GetQuestionsByQuizAsync(Guid quizId, CancellationToken cancellationToken = default)
    {
        var questions = await unitOfWork.Questions.GetQuestionsByQuizAsync(quizId, cancellationToken);
        return await MapToQuestionDtosAsync(questions, cancellationToken);
    }

    public async ValueTask<IEnumerable<QuestionDto>> GetQuestionsByQuizIdsAsync(IEnumerable<Guid> quizIds, CancellationToken cancellationToken = default)
    {
        var questions = await unitOfWork.Questions.GetQuestionsByQuizIdsAsync(quizIds, cancellationToken);
        return await MapToQuestionDtosAsync(questions, cancellationToken);
    }
    
    public async ValueTask<Dictionary<Guid, int>> GetQuestionCountsByQuizIdsAsync(IEnumerable<Guid> quizIds, CancellationToken cancellationToken = default) => 
        await unitOfWork.Questions.GetQuestionCountsByQuizIdsAsync(quizIds, cancellationToken);

    public async ValueTask<QuestionDto> CreateQuestionAsync(CreateQuestionRequest request, Guid userId, CancellationToken cancellationToken = default)
    {
        await createValidator.ValidateAndThrowAsync(request, cancellationToken);

        var quiz = await ValidateQuizOwnershipAsync(request.QuizId, userId, cancellationToken);
        await ValidateContentUniquenessAsync(request.Content, request.QuizId, cancellationToken: cancellationToken);

        await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var question = await CreateQuestionEntityAsync(request, cancellationToken);
            
            await unitOfWork.Questions.AddAsync(question, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);

            // Reload with quiz information
            question = await unitOfWork.Questions.GetByIdWithQuizAsync(question.Id, cancellationToken);
            return await MapToQuestionDtoAsync(question!, cancellationToken);
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async ValueTask<QuestionDto> UpdateQuestionAsync(Guid id, UpdateQuestionRequest request, Guid userId, CancellationToken cancellationToken = default)
    {
        await updateValidator.ValidateAndThrowAsync(request, cancellationToken);

        var question = await GetQuestionWithOwnershipValidationAsync(id, userId, cancellationToken);
        await ValidateContentUniquenessAsync(request.Content, question.QuizId, id, cancellationToken);

        await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var oldImagePublicId = question.ImagePublicId;
            
            await UpdateQuestionEntityAsync(question, request, cancellationToken);
            
            await unitOfWork.Questions.UpdateAsync(question, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            // Clean up old image if necessary
            await CleanupOldImageAsync(request.Image, oldImagePublicId, cancellationToken);

            await unitOfWork.CommitTransactionAsync(cancellationToken);

            // Reload with quiz
            question = await unitOfWork.Questions.GetByIdWithQuizAsync(question.Id, cancellationToken);
            return await MapToQuestionDtoAsync(question!, cancellationToken);
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async ValueTask<bool> DeleteQuestionAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var question = await unitOfWork.Questions.GetByIdWithQuizAsync(id, cancellationToken);
        if (question == null) return false;

        if (question.Quiz.CreatorId != userId)
            throw new UnauthorizedAccessException("You can only delete questions from your own quizzes");

        await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await DeleteQuestionWithCleanupAsync(question, cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);
            return true;
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async ValueTask<bool> BulkDeleteQuestionsAsync(IEnumerable<Guid> questionIds, Guid userId, CancellationToken cancellationToken = default)
    {
        var questionIdList = questionIds as List<Guid> ?? questionIds.ToList();
        if (questionIdList.Count == 0) return false;
        
        var questions = await unitOfWork.Questions.GetQuestionsByQuizIdsAsync(questionIdList, cancellationToken);
        var questionList = questions.ToList();
        
        if (questionList.Any(q => q.Quiz.CreatorId != userId))
            throw new UnauthorizedAccessException("You can only delete questions from the own quizzes");
        
        await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await unitOfWork.Questions.BulkDeleteAsync(questionList,
                cancellationToken); //// Todo: ????? cái này chấm hỏi vãi
            await unitOfWork.SaveChangesAsync(cancellationToken);
            var imagePublicIds = questionList.Select(q => q.ImagePublicId).ToList();
            var imageCleanupTasks = imagePublicIds
                .Where(id => !string.IsNullOrEmpty(id))
                .Select(async imagePublicId =>
                {
                    await ConcurrencySemaphore.WaitAsync(cancellationToken);
                    try
                    {
                        await imageService.DeleteImageAsync(imagePublicId!, cancellationToken);
                    }
                    catch
                    {
                        ConcurrencySemaphore.Release();
                    }
                });
            await Task.WhenAll(imageCleanupTasks);
            await unitOfWork.CommitTransactionAsync(cancellationToken);

            return true;
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
    
    public async ValueTask<IEnumerable<QuestionDto>> BulkUpdateQuestionsAsync(IEnumerable<UpdateQuestionBulkItem> requests, Guid userId, CancellationToken cancellationToken = default)
    {
        var requestList = requests as List<UpdateQuestionBulkItem> ?? requests.ToList();
        if (requestList.Count == 0) return [];

        var validationTasks = requestList.Select(async request =>
        {
            await updateValidator.ValidateAndThrowAsync(request.UpdateRequest, cancellationToken);
            return request;
        });
        
        await Task.WhenAll(validationTasks);
        
        var questionIds = requestList.Select(r => r.QuestionId).ToList();
        var questions = await unitOfWork.Questions.GetQuestionsByQuizIdsAsync(questionIds, cancellationToken);
        var questionDict = questions.ToDictionary(q => q.Id);
        
        if (questionDict.Values.Any(q => q.Quiz.CreatorId != userId))
            throw new UnauthorizedAccessException("You can only delete questions from the own quizzes");
        
        await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var updateTasks = requestList.Select(async request =>
            {
                if (!questionDict.TryGetValue(request.QuestionId, out var question))
                    throw new InvalidOperationException($"Question {request.QuestionId} not found");

                await ConcurrencySemaphore.WaitAsync(cancellationToken);
                try
                {
                    await UpdateQuestionEntityAsync(question, request.UpdateRequest, cancellationToken);
                    return question;
                }
                finally
                {
                    ConcurrencySemaphore.Release();
                }
            });
            
            var updatedQuestions = await Task.WhenAll(updateTasks);
            
            await unitOfWork.Questions.BulkUpdateAsync(updatedQuestions, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);
            
            var dtoTasks = updatedQuestions.Select(q => MapToQuestionDtoAsync(q, cancellationToken));
            return await Task.WhenAll(dtoTasks);
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
    
    public async ValueTask<bool> QuestionExistsAsync(Guid id, CancellationToken cancellationToken = default) => 
        await unitOfWork.Questions.ExistsAsync(q => q.Id == id, cancellationToken);


    public async ValueTask<BulkCreateQuestionsResponse> BulkCreateQuestionsAsync(BulkCreateQuestionsRequest request, Guid userId, CancellationToken cancellationToken = default)
    {
        await bulkCreateValidator.ValidateAndThrowAsync(request, cancellationToken);
        var quiz = await ValidateQuizOwnershipAsync(request.QuizId, userId, cancellationToken);
        var response = new BulkCreateQuestionsResponse { TotalProcessed = request.Questions.Count };
        var (validQuestions, failedQuestions) = await ValidateQuestionsInParallelAsync(request.Questions, request.QuizId, cancellationToken);
        response.FailedQuestions.AddRange(failedQuestions);
        if (validQuestions.Count == 0)
        {
            response.SuccessCount = 0;
            response.FailureCount = response.FailedQuestions.Count;
            return response;
        }
        await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var questions = await ProcessQuestionsWithImagesAsync(validQuestions, request.QuizId, cancellationToken);
            await unitOfWork.Questions.BulkCreateQuestionsAsync(questions, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            var dtosTask = questions.Select(async question =>
            {
                question.Quiz = quiz;
                return await MapToQuestionDtoAsync(question, cancellationToken);
            });
            response.SuccessfulQuestions = (await Task.WhenAll(dtosTask)).ToList();
            response.SuccessCount = response.SuccessfulQuestions.Count;
            response.FailureCount = response.FailedQuestions.Count;

            await unitOfWork.CommitTransactionAsync(cancellationToken);
            return response;
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    private async ValueTask<Quiz> ValidateQuizOwnershipAsync(Guid quizId, Guid userId,
        CancellationToken cancellationToken = default)
    {
        var quiz = await unitOfWork.Quizzes.GetByIdWithCreatorAsync(quizId, cancellationToken);
        if (quiz == null)
            throw new InvalidOperationException($"Quiz with id {quizId} does not exist");
        if (quiz.CreatorId != userId)
            throw new UnauthorizedAccessException($"Quiz with id {quizId} does not own {userId}");
        
        return quiz;
    }

    private async ValueTask ValidateContentUniquenessAsync(string content, Guid quizId, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var contentExists = await unitOfWork.Questions.ExistsByContentAndQuizAsync(content, quizId, excludeId, cancellationToken);
        if (contentExists)
            throw new InvalidOperationException("A question with this content already exists in this quiz");
    }
    
    private async ValueTask<Question> GetQuestionWithOwnershipValidationAsync(Guid id, Guid userId, CancellationToken cancellationToken)
    {
        var question = await unitOfWork.Questions.GetByIdWithQuizAsync(id, cancellationToken);
        if (question == null)
            throw new InvalidOperationException("Question not found");

        if (question.Quiz.CreatorId != userId)
            throw new UnauthorizedAccessException("You can only update questions in your own quizzes");

        return question;
    }

    private async ValueTask<Question> CreateQuestionEntityAsync(CreateQuestionRequest request,
        CancellationToken cancellationToken)
    {
        var question = new Question
        {
            Content = request.Content,
            QuestionType = request.QuestionType,
            TimeLimitInSeconds = request.TimeLimitInSeconds,
            Points = request.Points,
            Configuration = request.Configuration,
            Explanation = request.Explanation,
            QuizId = request.QuizId
        };

        if (request.Image != null)
        {
            var imageResponse = await imageService.UploadImageAsync(request.Image, QuestionFolder, cancellationToken);
            question.ImagePublicId = imageResponse.PublicId;
            question.ImageUrl = imageResponse.SecureUrl;
        }
        
        return question;
    }
    
    private async ValueTask UpdateQuestionEntityAsync(Question question, UpdateQuestionRequest request, CancellationToken cancellationToken)
    {
        question.Content = request.Content;
        question.QuestionType = request.QuestionType;
        question.TimeLimitInSeconds = request.TimeLimitInSeconds;
        question.Points = request.Points;
        question.Configuration = request.Configuration;
        question.Explanation = request.Explanation;

        if (request.Image != null)
        {
            var imageResponse = await imageService.UploadImageAsync(request.Image, QuestionFolder, cancellationToken);
            question.ImagePublicId = imageResponse.PublicId;
            question.ImageUrl = imageResponse.SecureUrl;
        }
    }
    
    private async ValueTask CleanupOldImageAsync(IFormFile? newImage, string? oldImagePublicId, CancellationToken cancellationToken)
    {
        if (newImage != null && !string.IsNullOrEmpty(oldImagePublicId))
            await imageService.DeleteImageAsync(oldImagePublicId, cancellationToken);
    }
    
    private async ValueTask DeleteQuestionWithCleanupAsync(Question question, CancellationToken cancellationToken)
    {
        var imagePublicId = question.ImagePublicId;

        await unitOfWork.Questions.DeleteAsync(question, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Delete image if exists
        if (!string.IsNullOrEmpty(imagePublicId))
            await imageService.DeleteImageAsync(imagePublicId, cancellationToken);
    }

    private async ValueTask<(List<CreateQuestionItem> Valid, List<QuestionCreationError> Failed)>
        ValidateQuestionsInParallelAsync(IEnumerable<CreateQuestionItem> questions, Guid quizId,
            CancellationToken cancellationToken)
    {
        var questionList = questions as List<CreateQuestionItem> ?? questions.ToList();
        var validationTasks = questionList.Select(async (questionItem, index) =>
        {
            var errors = new List<string>();
            var contentExists = await unitOfWork.Questions.ExistsByContentAndQuizAsync(questionItem.Content, quizId, cancellationToken: cancellationToken);
            if (contentExists)
                errors.Add("A question with this content already exists in this quiz");
            return new { Index = index, QuestionItem = questionItem, Errors = errors };
        });
        var validationResults = await Task.WhenAll(validationTasks);
        var validQuestion = validationResults
            .Where(r => r.Errors.Count == 0)
            .Select(r => r.QuestionItem)
            .ToList();
        var failedQuestions = validationResults
            .Where(r => r.Errors.Count > 0)
            .Select(r => new QuestionCreationError
            {
                Order = r.QuestionItem.Order,
                Content = r.QuestionItem.Content,
                Errors = r.Errors
            })
            .ToList();
        
        return (validQuestion, failedQuestions);
    }

    private async ValueTask<List<Question>> ProcessQuestionsWithImagesAsync(
        IEnumerable<CreateQuestionItem> validQuestions, Guid quizId, CancellationToken cancellationToken)
    {
        var questionList = validQuestions as List<CreateQuestionItem> ?? validQuestions.ToList();

        var imageUploadTasks = questionList.Select(async questionItem =>
        {
            await ConcurrencySemaphore.WaitAsync(cancellationToken);
            try
            {
                string? imagePublicId = null;
                string? imageUrl = null;

                if (!string.IsNullOrEmpty(questionItem.ImageBase64))
                {
                    var imagesBytes = Convert.FromBase64String(questionItem.ImageBase64);
                    var fileName = questionItem.ImageFileName ?? $"question_{Guid.NewGuid()}.jpg";
                    using var imageStream = new MemoryStream(imagesBytes);
                    var formFile = CreateFormFileFromStream(imageStream, fileName, "image/jpeg");
                    var imageResponse =
                        await imageService.UploadImageAsync(formFile, QuestionFolder, cancellationToken);
                    imagePublicId = imageResponse.PublicId;
                    imageUrl = imageResponse.SecureUrl;
                }

                return new Question
                {
                    Content = questionItem.Content,
                    QuestionType = questionItem.QuestionType,
                    TimeLimitInSeconds = questionItem.TimeLimitInSeconds,
                    Points = questionItem.Points,
                    Configuration = questionItem.Configuration,
                    Explanation = questionItem.Explanation,
                    QuizId = quizId,
                    ImagePublicId = imagePublicId,
                    ImageUrl = imageUrl
                };
            }
            finally
            {
                ConcurrencySemaphore.Release();
            }
        });

        return (await Task.WhenAll(imageUploadTasks)).ToList();
    }

    private async ValueTask<IEnumerable<QuestionDto>> MapToQuestionDtosAsync(IEnumerable<Question> questions,
        CancellationToken cancellationToken)
    {
        var questionList = questions as List<Question> ?? questions.ToList();
        var dtoTasks = questionList.Select(q => MapToQuestionDtoAsync(q, cancellationToken));
        return await Task.WhenAll(dtoTasks);
    }
    
    private async Task<QuestionDto> MapToQuestionDtoAsync(Question question, CancellationToken cancellationToken)
    {
        var dto = new QuestionDto
        {
            Id = question.Id,
            Content = question.Content,
            QuestionType = question.QuestionType,
            TimeLimitInSeconds = question.TimeLimitInSeconds,
            Points = question.Points,
            ImageUrl = question.ImageUrl,
            Configuration = question.Configuration,
            Explanation = question.Explanation,
            CreatedAt = question.CreatedAt,
            UpdatedAt = question.UpdatedAt,
            QuizId = question.QuizId,
            QuizTitle = question.Quiz?.Title ?? string.Empty
        };

        // Add image transformations if image exists
        if (!string.IsNullOrEmpty(question.ImagePublicId))
        {
            var transformationTasks = new[]
            {
                ("small", "c_fill,w_200,h_150,q_auto,f_auto"),
                ("medium", "c_fill,w_400,h_300,q_auto,f_auto"),
                ("large", "c_fill,w_800,h_600,q_auto,f_auto")
            }.Select(async t => new
            {
                Key = t.Item1,
                Url = await imageService.GetImageUrlWithTransformationAsync(question.ImagePublicId, t.Item2,
                    cancellationToken)
            });
            
            var transformations = await Task.WhenAll(transformationTasks);
            dto.ImageTransformations = transformations.ToDictionary(t => t.Key, t => t.Url);
        }

        return dto;
    }
    
    private static IFormFile CreateFormFileFromStream(Stream stream, string fileName, string contentType) =>
        new FormFile(stream, 0, stream.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
} 