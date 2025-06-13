namespace Nemui.Shared.Constants;

public static class QuizConstants
{
    public static class FieldLengths
    {
        public const int QuizTitleMaxLength = 200;
        public const int QuizDescriptionMaxLength = 1000;
        public const int QuizCategoryMaxLength = 100;
        public const int QuestionContentMaxLength = 2000;
        public const int QuestionExplanationMaxLength = 1000;
        public const int PlayerNicknameMaxLength = 50;
        public const int SessionCodeLength = 6;
        public const int ConfigurationMaxLength = 10000;
        public const int PlayerAnswerMaxLength = 5000;
    }

    public static class Defaults
    {
        public const int DefaultQuestionTimeLimit = 30;
        public const int DefaultQuestionPoints = 100;
        public const int MaxPlayersPerSession = 100;
        public const int SessionCodeExpiry = 24; 
    }

    public static class TableNames
    {
        public const string Quizzes = "Quizzes";
        public const string Questions = "Questions";
        public const string GameSessions = "GameSessions";
        public const string Players = "Players";
        public const string PlayerAnswers = "PlayerAnswers";
    }

    public static class Indexes
    {
        public const string QuizCreatorIdIndex = "IX_Quiz_CreatorId";
        public const string QuizIsPublicIndex = "IX_Quiz_IsPublic";
        public const string QuizCategoryIndex = "IX_Quiz_Category";
        public const string QuestionQuizIdIndex = "IX_Question_QuizId";
        public const string GameSessionCodeIndex = "IX_GameSession_SessionCode";
        public const string GameSessionHostIdIndex = "IX_GameSession_HostId";
        public const string PlayerGameSessionIdIndex = "IX_Player_GameSessionId";
        public const string PlayerUserIdIndex = "IX_Player_UserId";
        public const string PlayerAnswerPlayerIdIndex = "IX_PlayerAnswer_PlayerId";
        public const string PlayerAnswerQuestionIdIndex = "IX_PlayerAnswer_QuestionId";
        public const string PlayerAnswerGameSessionIdIndex = "IX_PlayerAnswer_GameSessionId";
    }
}