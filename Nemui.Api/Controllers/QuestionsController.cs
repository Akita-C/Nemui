using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nemui.Application.Services;
using Nemui.Shared.DTOs.Common;
using Nemui.Shared.DTOs.Quiz;

namespace Nemui.Api.Controllers;

[Authorize]
[ApiExplorerSettings(GroupName = "Questions")]
public class QuestionsController : BaseApiController
{
    private readonly IQuestionService _questionService;
    private readonly ILogger<QuestionsController> _logger;

    public QuestionsController(IQuestionService questionService, ILogger<QuestionsController> logger)
    {
        _questionService = questionService;
        _logger = logger;
    }
    
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<QuestionDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetQuestionsAsync([FromQuery] QuestionListRequest request, CancellationToken cancellationToken = default) =>
        await ExecuteWithErrorHandlingAsync(async () =>
        {
            var (questions, nextCursor) = await _questionService.GetQuestionsAsync(request, cancellationToken);
            var pagedResponse = CreatePagedResponse(questions.ToList(), nextCursor);
            
            _logger.LogInformation("Retrieved {Count} questions", pagedResponse.Data.Count);
            return Ok(ApiResponse<PagedResponse<QuestionDto>>.SuccessResult(pagedResponse, "Questions retrieved successfully"));
        }, "retrieving questions", _logger);

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<QuestionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetQuestionByIdAsync(
        [Required] Guid id, 
        CancellationToken cancellationToken = default) =>
        await ExecuteWithErrorHandlingAsync(async () =>
            {
                var question = await _questionService.GetQuestionByIdAsync(id, cancellationToken);
                if (question == null)
                {
                    _logger.LogWarning("Question {QuestionId} not found", id);
                    return NotFound(ErrorResponse.Create("Question not found"));
                }

                _logger.LogInformation("Retrieved question {QuestionId}", id);
                return Ok(ApiResponse<QuestionDto>.SuccessResult(question, "Question retrieved successfully"));
            }, $"retrieving question {id}", _logger);

    [HttpGet("quiz/{quizId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<QuestionDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetQuestionsByQuizAsync(
        [Required] Guid quizId, 
        CancellationToken cancellationToken = default) =>
        await ExecuteWithErrorHandlingAsync(async () =>
            {
                var questions = await _questionService.GetQuestionsByQuizAsync(quizId, cancellationToken);
                var questionList = questions.ToList();
            
                _logger.LogInformation("Retrieved {Count} questions for quiz {QuizId}", questionList.Count, quizId);
                return Ok(ApiResponse<IEnumerable<QuestionDto>>.SuccessResult(questionList, "Quiz questions retrieved successfully"));
            }, $"retrieving questions for quiz {quizId}", _logger);

    [HttpPost("batch/by-quizzes")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<QuestionDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetQuestionsByQuizIdsAsync(
        [FromBody] [Required] IEnumerable<Guid> quizIds, 
        CancellationToken cancellationToken = default) =>
        await ExecuteWithErrorHandlingAsync(async () =>
        {
            var quizIdList = quizIds.ToList();
            if (quizIdList.Count == 0)
                return BadRequest(ErrorResponse.Create("At least one quiz ID must be provided"));

            if (quizIdList.Count > 50)
                return BadRequest(ErrorResponse.Create("Maximum 50 quiz IDs allowed per request"));

            var questions = await _questionService.GetQuestionsByQuizIdsAsync(quizIdList, cancellationToken);
            var questionList = questions.ToList();
            
            _logger.LogInformation("Retrieved {Count} questions for {QuizCount} quizzes", questionList.Count, quizIdList.Count);
            return Ok(ApiResponse<IEnumerable<QuestionDto>>.SuccessResult(questionList, "Questions retrieved successfully"));
        }, "retrieving questions for multiple quizzes", _logger);

    [HttpPost("batch/counts")]
    [ProducesResponseType(typeof(ApiResponse<Dictionary<Guid, int>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetQuestionCountsByQuizIdsAsync(
        [FromBody] [Required] IEnumerable<Guid> quizIds, 
        CancellationToken cancellationToken = default) =>
        await ExecuteWithErrorHandlingAsync(async () =>
        {
            var quizIdList = quizIds.ToList();
            if (quizIdList.Count == 0)
                return BadRequest(ErrorResponse.Create("At least one quiz ID must be provided"));

            var counts = await _questionService.GetQuestionCountsByQuizIdsAsync(quizIdList, cancellationToken);
            
            _logger.LogInformation("Retrieved question counts for {QuizCount} quizzes", quizIdList.Count);
            return Ok(ApiResponse<Dictionary<Guid, int>>.SuccessResult(counts, "Question counts retrieved successfully"));
        }, "retrieving question counts", _logger);

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<QuestionDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateQuestionAsync(
        [FromForm] CreateQuestionRequest request, 
        CancellationToken cancellationToken = default) =>
        await ExecuteWithAuthenticationAsync(async userId =>
        {
            var question = await _questionService.CreateQuestionAsync(request, userId, cancellationToken);
            
            _logger.LogInformation("Question {QuestionId} created successfully by user {UserId}", question.Id, userId);
            return CreatedAtAction(nameof(GetQuestionByIdAsync), new { id = question.Id }, 
                ApiResponse<QuestionDto>.SuccessResult(question, "Question created successfully"));
        }, "creating question", _logger);

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<QuestionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateQuestionAsync(
        [Required] Guid id, 
        [FromForm] UpdateQuestionRequest request, 
        CancellationToken cancellationToken = default) =>
        await ExecuteWithAuthenticationAsync(async userId =>
            {
                var question = await _questionService.UpdateQuestionAsync(id, request, userId, cancellationToken);
            
                _logger.LogInformation("Question {QuestionId} updated successfully by user {UserId}", id, userId);
                return Ok(ApiResponse<QuestionDto>.SuccessResult(question, "Question updated successfully"));
            }, $"updating question {id}", _logger);
    
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteQuestionAsync(
        [Required] Guid id, 
        CancellationToken cancellationToken = default) =>
        await ExecuteWithAuthenticationAsync(async userId =>
            {
                var result = await _questionService.DeleteQuestionAsync(id, userId, cancellationToken);
            
                if (!result)
                {
                    _logger.LogWarning("Question {QuestionId} not found for deletion", id);
                    return NotFound(ErrorResponse.Create("Question not found"));
                }

                _logger.LogInformation("Question {QuestionId} deleted successfully by user {UserId}", id, userId);
                return Ok(ApiResponse<bool>.SuccessResult(true, "Question deleted successfully"));
            }, $"deleting question {id}", _logger);
    
    [HttpDelete("batch")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> BulkDeleteQuestionsAsync(
        [FromBody] [Required] IEnumerable<Guid> questionIds, 
        CancellationToken cancellationToken = default) =>
        await ExecuteWithAuthenticationAsync(async userId =>
        {
            var questionIdList = questionIds.ToList();
            if (questionIdList.Count == 0)
                return BadRequest(ErrorResponse.Create("At least one question ID must be provided"));

            if (questionIdList.Count > 100)
                return BadRequest(ErrorResponse.Create("Maximum 100 questions can be deleted at once"));

            var result = await _questionService.BulkDeleteQuestionsAsync(questionIdList, userId, cancellationToken);
            
            _logger.LogInformation("Bulk deleted {Count} questions by user {UserId}", questionIdList.Count, userId);
            return Ok(ApiResponse<bool>.SuccessResult(result, $"Successfully deleted {questionIdList.Count} questions"));
        }, "bulk deleting questions", _logger);
    
    [HttpPost("bulk")]
    [ProducesResponseType(typeof(ApiResponse<BulkCreateQuestionsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> BulkCreateQuestionsAsync(
        [FromBody] BulkCreateQuestionsRequest request, 
        CancellationToken cancellationToken = default) =>
        await ExecuteWithAuthenticationAsync(async userId =>
        {
            var result = await _questionService.BulkCreateQuestionsAsync(request, userId, cancellationToken);
            
            _logger.LogInformation("Bulk created {SuccessCount}/{TotalCount} questions for quiz {QuizId} by user {UserId}", 
                result.SuccessCount, result.TotalProcessed, request.QuizId, userId);

            var message = result.FailureCount > 0 
                ? $"Processed {result.TotalProcessed} questions. {result.SuccessCount} created successfully, {result.FailureCount} failed."
                : $"Successfully created all {result.SuccessCount} questions.";

            return Ok(ApiResponse<BulkCreateQuestionsResponse>.SuccessResult(result, message));
        }, "bulk creating questions", _logger);
    
    [HttpPut("batch")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<QuestionDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> BulkUpdateQuestionsAsync(
        [FromBody] [Required] IEnumerable<UpdateQuestionBulkItem> requests, 
        CancellationToken cancellationToken = default) =>
        await ExecuteWithAuthenticationAsync(async userId =>
        {
            var requestList = requests.ToList();
            if (requestList.Count == 0)
                return BadRequest(ErrorResponse.Create("At least one question update must be provided"));

            if (requestList.Count > 50)
                return BadRequest(ErrorResponse.Create("Maximum 50 questions can be updated at once"));

            var questions = await _questionService.BulkUpdateQuestionsAsync(requestList, userId, cancellationToken);
            var questionList = questions.ToList();
            
            _logger.LogInformation("Bulk updated {Count} questions by user {UserId}", questionList.Count, userId);
            return Ok(ApiResponse<IEnumerable<QuestionDto>>.SuccessResult(questionList, $"Successfully updated {questionList.Count} questions"));
        }, "bulk updating questions", _logger);
} 