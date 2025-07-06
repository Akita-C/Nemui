namespace Nemui.Application.Services.Games.Draw;

public interface IWordRevealService
{
    /// <summary>
    /// Reveal the word by percentage
    /// </summary>
    /// <param name="word">Original word</param>
    /// <param name="revealPercentage">Percentage of the word to reveal (0.0 - 1.0)</param>
    /// <returns>Reveal word with underscore for unrevealed letters</returns>
    string RevealWord(string word, float revealPercentage);
}