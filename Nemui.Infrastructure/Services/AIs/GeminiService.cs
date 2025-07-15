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
                You are a helpful assistant that generates related words. 
                Generate words that are semantically related to the given base word.
                Ensure all words are relevant and appropriate.
                """;

            var userPrompt = $"Generate {wordCount} words related to '{baseWord}'";

            var executionSettings = new GeminiPromptExecutionSettings
            {
                ResponseSchema = typeof(T),
                ResponseMimeType = "application/json",
                MaxTokens = 1000,
                Temperature = 0.7
            };

            var chatHistory = new ChatHistory(systemPrompt);
            chatHistory.AddUserMessage(userPrompt);

            logger.LogInformation("Base word: {BaseWord}, Word count: {WordCount}", baseWord, wordCount);

            var result = await chatCompletionService.GetChatMessageContentAsync(chatHistory, executionSettings);

            logger.LogInformation("Generated words: {Words}", result.Content);

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