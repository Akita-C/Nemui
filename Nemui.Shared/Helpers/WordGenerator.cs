namespace Nemui.Shared.Helpers;

public static class WordGenerator
{
    private static readonly List<string> SampleWords = [
        "apple", "banana", "cat", "dog", "elephant", "flower", "guitar", "house", "island", "jungle",
        "kite", "lemon", "mountain", "notebook", "orange", "pencil", "queen", "river", "sun", "tree",
        "umbrella", "violin", "window", "xylophone", "yacht", "zebra"
    ];

    private static readonly Random Random = new();

    // Absolutely can be improved, right now 100% can be infinite loop
    public static List<string> GenerateRandomWords(int count)
    {
        var result = new HashSet<string>();
        while (result.Count < count)
        {
            var word = SampleWords[Random.Next(SampleWords.Count)];
            while (!result.Add(word))
            {
                word = SampleWords[Random.Next(SampleWords.Count)];
            }
        }
        return [.. result];
    }
}