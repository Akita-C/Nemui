using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;
using Nemui.Application.Services;

namespace Nemui.Infrastructure.Services.AIs;

public class GeminiService(IChatCompletionService chatCompletionService, ILogger<GeminiService> logger) : IAIService
{
    public async Task<T> GenerateWords<T>(string baseWord, int wordCount) where T : class
    {
        try
        {
            var systemPrompt = """
            You are a helpful assistant that generates words semantically related to a given base word or theme. 
            Generate words that are directly relevant to the specified theme, covering diverse aspects of the theme to ensure variety (e.g., for 'Animals', include mammals, birds, reptiles, etc.). 
            Ensure all words are unique, appropriate, and specific to the theme, avoiding overly generic terms. 
            Prioritize clarity and richness in meaning, tailoring the words to reflect distinct characteristics or categories within the theme.
            """;

            var userPrompt = $"Generate {wordCount} unique words closely related to the theme '{baseWord}'. Ensure the words are diverse, specific, and capture various facets of the theme.";

            var executionSettings = new GeminiPromptExecutionSettings
            {
                ResponseSchema = typeof(T),
                ResponseMimeType = "application/json",
                MaxTokens = 1000,
                Temperature = 0.5
            };

            var chatHistory = new ChatHistory(systemPrompt);
            chatHistory.AddUserMessage(userPrompt);

            var result = await chatCompletionService.GetChatMessageContentAsync(chatHistory, executionSettings);

            if (result.Content is null)
                throw new InvalidOperationException("Failed to generate words");

            return JsonSerializer.Deserialize<T>(result.Content)!;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to generate words");
            throw;
        }

    }
}