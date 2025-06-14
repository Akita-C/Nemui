using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nemui.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Add_Quiz_Question_Player_PlayerAnswer_GameSession_tables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Quizzes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ThumbnailUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ThumbnailPublicId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsPublic = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Tags = table.Column<string>(type: "jsonb", nullable: true),
                    EstimatedDurationMinutes = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quizzes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Quizzes_Users_CreatorId",
                        column: x => x.CreatorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GameSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionCode = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Settings = table.Column<string>(type: "jsonb", nullable: true),
                    QuizId = table.Column<Guid>(type: "uuid", nullable: false),
                    HostId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameSessions_Quizzes_QuizId",
                        column: x => x.QuizId,
                        principalTable: "Quizzes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GameSessions_Users_HostId",
                        column: x => x.HostId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Questions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Content = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    QuestionType = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    TimeLimitInSeconds = table.Column<int>(type: "integer", nullable: false, defaultValue: 30),
                    Points = table.Column<int>(type: "integer", nullable: false, defaultValue: 100),
                    ImageUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ImagePublicId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Configuration = table.Column<string>(type: "jsonb", maxLength: 10000, nullable: true),
                    Explanation = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    QuizId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Questions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Questions_Quizzes_QuizId",
                        column: x => x.QuizId,
                        principalTable: "Quizzes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Players",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nickname = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TotalScore = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    JoinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsConnected = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    ConnectionId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    GameSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Players", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Players_GameSessions_GameSessionId",
                        column: x => x.GameSessionId,
                        principalTable: "GameSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Players_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PlayerAnswers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    GameSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Answer = table.Column<string>(type: "jsonb", maxLength: 5000, nullable: true),
                    TimeTakenInSeconds = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    PointsEarned = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    IsCorrect = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    AnsweredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerAnswers_GameSessions_GameSessionId",
                        column: x => x.GameSessionId,
                        principalTable: "GameSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayerAnswers_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayerAnswers_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GameSession_HostId",
                table: "GameSessions",
                column: "HostId");

            migrationBuilder.CreateIndex(
                name: "IX_GameSession_SessionCode",
                table: "GameSessions",
                column: "SessionCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GameSessions_CreatedAt",
                table: "GameSessions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_GameSessions_IsDeleted",
                table: "GameSessions",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_GameSessions_QuizId",
                table: "GameSessions",
                column: "QuizId");

            migrationBuilder.CreateIndex(
                name: "IX_GameSessions_StartedAt",
                table: "GameSessions",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerAnswer_GameSessionId",
                table: "PlayerAnswers",
                column: "GameSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerAnswer_PlayerId",
                table: "PlayerAnswers",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerAnswer_QuestionId",
                table: "PlayerAnswers",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerAnswers_AnsweredAt",
                table: "PlayerAnswers",
                column: "AnsweredAt");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerAnswers_CreatedAt",
                table: "PlayerAnswers",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerAnswers_IsDeleted",
                table: "PlayerAnswers",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerAnswers_PlayerId_QuestionId",
                table: "PlayerAnswers",
                columns: new[] { "PlayerId", "QuestionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Player_GameSessionId",
                table: "Players",
                column: "GameSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Player_UserId",
                table: "Players",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Players_CreatedAt",
                table: "Players",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Players_GameSessionId_Nickname",
                table: "Players",
                columns: new[] { "GameSessionId", "Nickname" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Players_IsDeleted",
                table: "Players",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Players_JoinedAt",
                table: "Players",
                column: "JoinedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Question_QuizId",
                table: "Questions",
                column: "QuizId");

            migrationBuilder.CreateIndex(
                name: "IX_Questions_CreatedAt",
                table: "Questions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Questions_IsDeleted",
                table: "Questions",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Questions_QuestionType",
                table: "Questions",
                column: "QuestionType");

            migrationBuilder.CreateIndex(
                name: "IX_Quiz_Category",
                table: "Quizzes",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Quiz_CreatorId",
                table: "Quizzes",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_Quiz_IsPublic",
                table: "Quizzes",
                column: "IsPublic");

            migrationBuilder.CreateIndex(
                name: "IX_Quizzes_CreatedAt",
                table: "Quizzes",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Quizzes_IsDeleted",
                table: "Quizzes",
                column: "IsDeleted");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlayerAnswers");

            migrationBuilder.DropTable(
                name: "Players");

            migrationBuilder.DropTable(
                name: "Questions");

            migrationBuilder.DropTable(
                name: "GameSessions");

            migrationBuilder.DropTable(
                name: "Quizzes");
        }
    }
}
