namespace Nemui.Application.Services;

public interface IAIService
{
    Task<T> GenerateWords<T>(string baseWord, int wordCount) where T : class;
}