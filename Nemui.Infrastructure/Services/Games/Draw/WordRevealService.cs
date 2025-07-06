using Nemui.Application.Services.Games.Draw;

namespace Nemui.Infrastructure.Services.Games.Draw;

public class WordRevealService : IWordRevealService
{
    public string RevealWord(string word, float revealPercentage)
    {
        if (string.IsNullOrWhiteSpace(word)) return word;

        var letterPositions = word.Where(char.IsLetter).Select((_, i) => i).ToArray();
        if (letterPositions.Length == 0) return word;

        var lettersToReveal = (int)Math.Ceiling(letterPositions.Length * revealPercentage);
        lettersToReveal = Math.Min(lettersToReveal, letterPositions.Length);

        var random = new Random(word.GetHashCode());

        var positionsToReveal = letterPositions
            .OrderBy(x => random.Next())
            .Take(lettersToReveal)
            .ToHashSet();

        var result = new char[word.Length];
        result = [.. word.Select((wordChar, index) =>
        {
            if (char.IsLetter(wordChar))
            {
                return positionsToReveal.Contains(index) ? wordChar : '_';
            }

            // Giữ nguyên khoảng trắng và ký tự đặc biệt
            return wordChar;
        })];

        return new string(result);
    }
}