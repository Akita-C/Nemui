using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nemui.Application.Services;
using Nemui.Shared.DTOs.Common;
using Nemui.Shared.DTOs.Quiz;

namespace Nemui.Api.Controllers;

[Authorize]
public class QuizzesController : BaseApiController
{
    private readonly IQuizService _quizService;
    private readonly ILogger<QuizzesController> _logger;

    public QuizzesController(IQuizService quizService, ILogger<QuizzesController> logger)
    {
        _quizService = quizService;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<QuizSummaryDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetQuizzesAsync([FromQuery] QuizListRequest request, CancellationToken cancellationToken = default) =>
        await ExecuteWithErrorHandlingAsync(async () =>
        {
            var (quizzes, nextCursor) = await _quizService.GetQuizzesAsync(request, cancellationToken);
            var pagedResponse = CreatePagedResponse(quizzes.ToList(), nextCursor);
            
            _logger.LogInformation("Retrieved {Count} quizzes", pagedResponse.Data.Count);
            return Ok(ApiResponse<PagedResponse<QuizSummaryDto>>.SuccessResult(pagedResponse, "Quizzes retrieved successfully"));
        }, "retrieving quizzes", _logger);
    
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<QuizDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetQuizByIdAsync(
        [Required] Guid id, 
        CancellationToken cancellationToken = default) =>
        await ExecuteWithErrorHandlingAsync(async () =>
            {
                var quiz = await _quizService.GetQuizByIdAsync(id, cancellationToken);
                if (quiz == null)
                {
                    _logger.LogWarning("Quiz {QuizId} not found", id);
                    return NotFound(ErrorResponse.Create("Quiz not found"));
                }

                _logger.LogInformation("Retrieved quiz {QuizId}", id);
                return Ok(ApiResponse<QuizDto>.SuccessResult(quiz, "Quiz retrieved successfully"));
            }, $"retrieving quiz {id}", _logger);

    [HttpGet("my")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<QuizSummaryDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetMyQuizzesAsync(CancellationToken cancellationToken = default) =>
        await ExecuteWithAuthenticationAsync(async userId =>
        {
            var quizzes = await _quizService.GetMyQuizzesAsync(userId, cancellationToken);
            var quizList = quizzes.ToList();
            
            _logger.LogInformation("Retrieved {Count} quizzes for user {UserId}", quizList.Count, userId);
            return Ok(ApiResponse<IEnumerable<QuizSummaryDto>>.SuccessResult(quizList, "Your quizzes retrieved successfully"));
        }, "retrieving user quizzes", _logger);

    [HttpGet("public")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<QuizSummaryDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPublicQuizzesAsync(
        [FromQuery] [Range(1, 50)] int limit = 10, 
        CancellationToken cancellationToken = default) =>
        await ExecuteWithErrorHandlingAsync(async () =>
        {
            var quizzes = await _quizService.GetPublicQuizzesAsync(limit, cancellationToken);
            var quizList = quizzes.ToList();
            
            _logger.LogInformation("Retrieved {Count} public quizzes", quizList.Count);
            return Ok(ApiResponse<IEnumerable<QuizSummaryDto>>.SuccessResult(quizList, "Public quizzes retrieved successfully"));
        }, "retrieving public quizzes", _logger);

    [HttpGet("category/{category}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<QuizSummaryDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetQuizzesByCategoryAsync(
        [Required] string category, 
        [FromQuery] [Range(1, 50)] int limit = 20, 
        CancellationToken cancellationToken = default) =>
        await ExecuteWithErrorHandlingAsync(async () =>
            {
                if (string.IsNullOrWhiteSpace(category))
                    return BadRequest(ErrorResponse.Create("Category cannot be empty"));

                var quizzes = await _quizService.GetQuizzesByCategoryAsync(category, limit, cancellationToken);
                var quizList = quizzes.ToList();
            
                _logger.LogInformation("Retrieved {Count} quizzes for category {Category}", quizList.Count, category);
                return Ok(ApiResponse<IEnumerable<QuizSummaryDto>>.SuccessResult(quizList, $"Quizzes in category '{category}' retrieved successfully"));
            }, $"retrieving quizzes for category {category}", _logger);

    [HttpGet("search")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<QuizSummaryDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SearchQuizzesAsync(
        [FromQuery] [Required] [MinLength(2)] string q, 
        [FromQuery] [Range(1, 50)] int limit = 20, 
        CancellationToken cancellationToken = default) =>
        await ExecuteWithErrorHandlingAsync(async () =>
            {
                var quizzes = await _quizService.SearchQuizzesAsync(q, limit, cancellationToken);
                var quizList = quizzes.ToList();
            
                _logger.LogInformation("Found {Count} quizzes for search term '{SearchTerm}'", quizList.Count, q);
                return Ok(ApiResponse<IEnumerable<QuizSummaryDto>>.SuccessResult(quizList, $"Search results for '{q}'"));
            }, $"searching quizzes with term '{q}'", _logger);

    [HttpGet("categories/counts")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<Dictionary<string, int>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetQuizCountsByCategoryAsync(CancellationToken cancellationToken = default) =>
        await ExecuteWithErrorHandlingAsync(async () =>
        {
            var counts = await _quizService.GetQuizCountsByCategoryAsync(cancellationToken);
            
            _logger.LogInformation("Retrieved quiz counts for {CategoryCount} categories", counts.Count);
            return Ok(ApiResponse<Dictionary<string, int>>.SuccessResult(counts, "Category counts retrieved successfully"));
        }, "retrieving category counts", _logger);
    
    [HttpPost("batch")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<QuizDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetQuizzesByIdsAsync(
        [FromBody] [Required] IEnumerable<Guid> quizIds, 
        CancellationToken cancellationToken = default) =>
        await ExecuteWithErrorHandlingAsync(async () =>
        {
            var quizIdList = quizIds.ToList();
            if (quizIdList.Count == 0)
                return BadRequest(ErrorResponse.Create("At least one quiz ID must be provided"));

            if (quizIdList.Count > 50)
                return BadRequest(ErrorResponse.Create("Maximum 50 quiz IDs allowed per request"));

            var quizzes = await _quizService.GetQuizzesByIdsAsync(quizIdList, cancellationToken);
            var quizList = quizzes.ToList();
            
            _logger.LogInformation("Retrieved {Count} quizzes for {RequestCount} IDs", quizList.Count, quizIdList.Count);
            return Ok(ApiResponse<IEnumerable<QuizDto>>.SuccessResult(quizList, "Quizzes retrieved successfully"));
        }, "retrieving quizzes by IDs", _logger);
    
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<QuizDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateQuizAsync(
        [FromForm] CreateQuizRequest request, 
        CancellationToken cancellationToken = default) =>
        await ExecuteWithAuthenticationAsync(async userId =>
        {
            var quiz = await _quizService.CreateQuizAsync(request, userId, cancellationToken);
            
            _logger.LogInformation("Quiz {QuizId} created successfully by user {UserId}", quiz.Id, userId);
            return CreatedAtAction(nameof(GetQuizByIdAsync), new { id = quiz.Id }, 
                ApiResponse<QuizDto>.SuccessResult(quiz, "Quiz created successfully"));
        }, "creating quiz", _logger);
    
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<QuizDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateQuizAsync(
        [Required] Guid id, 
        [FromForm] UpdateQuizRequest request, 
        CancellationToken cancellationToken = default) =>
        await ExecuteWithAuthenticationAsync(async userId =>
            {
                var quiz = await _quizService.UpdateQuizAsync(id, request, userId, cancellationToken);
            
                _logger.LogInformation("Quiz {QuizId} updated successfully by user {UserId}", id, userId);
                return Ok(ApiResponse<QuizDto>.SuccessResult(quiz, "Quiz updated successfully"));
            }, $"updating quiz {id}", _logger);
    
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteQuizAsync(
        [Required] Guid id, 
        CancellationToken cancellationToken = default) =>
        await ExecuteWithAuthenticationAsync(async userId =>
            {
                var result = await _quizService.DeleteQuizAsync(id, userId, cancellationToken);
            
                if (!result)
                {
                    _logger.LogWarning("Quiz {QuizId} not found for deletion", id);
                    return NotFound(ErrorResponse.Create("Quiz not found"));
                }

                _logger.LogInformation("Quiz {QuizId} deleted successfully by user {UserId}", id, userId);
                return Ok(ApiResponse<bool>.SuccessResult(true, "Quiz deleted successfully"));
            }, $"deleting quiz {id}", _logger);
    
    [HttpDelete("batch")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> BulkDeleteQuizzesAsync(
        [FromBody] [Required] IEnumerable<Guid> quizIds, 
        CancellationToken cancellationToken = default) =>
        await ExecuteWithAuthenticationAsync(async userId =>
        {
            var quizIdList = quizIds.ToList();
            if (quizIdList.Count == 0)
                return BadRequest(ErrorResponse.Create("At least one quiz ID must be provided"));

            if (quizIdList.Count > 50)
                return BadRequest(ErrorResponse.Create("Maximum 50 quizzes can be deleted at once"));

            var result = await _quizService.BulkDeleteQuizzesAsync(quizIdList, userId, cancellationToken);
            
            _logger.LogInformation("Bulk deleted {Count} quizzes by user {UserId}", quizIdList.Count, userId);
            return Ok(ApiResponse<bool>.SuccessResult(result, $"Successfully deleted {quizIdList.Count} quizzes"));
        }, "bulk deleting quizzes", _logger);
}