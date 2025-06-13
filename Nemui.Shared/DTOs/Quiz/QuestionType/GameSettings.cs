namespace Nemui.Shared.DTOs.Quiz;

public class GameSettings
{
    public bool ShowLeaderboard { get; set; } = true;
    public bool ShowAnswersAfterQuestion { get; set; } = true;
    public int QuestionTransitionTimeSeconds { get; set; } = 5;
    public bool ShuffleQuestions { get; set; } = false;
    public bool AutoAdvanceQuestions { get; set; } = true;
}